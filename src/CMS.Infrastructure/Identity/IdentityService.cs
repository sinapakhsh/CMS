using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CMS.Application.Common.Abstractions;
using CMS.Application.Identity;
using CMS.Domain.Identity.Sessions;
using CMS.Domain.Identity.Users;
using CMS.Infrastructure.Persistence.Write;
using CMS.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CMS.Infrastructure.Identity;

// Credential flows use their own tenant scope after resolving an existing tenant by slug.
public sealed class IdentityService(DbContextOptions<CmsDbContext> databaseOptions, ICurrentTenant currentTenant,
    ICurrentUser currentUser, IClock clock, IIdGenerator ids, IOptions<JwtOptions> jwt) : IIdentityService
{
    private readonly PasswordHasher<User> _hasher = new();
    private static readonly Error _invalid = new("Identity.InvalidCredentials", "Credentials or session are invalid.");

    // Email and password bounds are enforced before hashing or database work.
    public async Task<Result<Guid>> RegisterAsync(string tenant, string email, string password, CancellationToken cancellationToken)
    {
        if (!ValidEmail(email) || !ValidPassword(password))
            return Result<Guid>.Failure(new Error("Identity.Invalid", "A valid email and password of 12 to 128 characters are required."));
        var tenantId = await ResolveTenantAsync(tenant, cancellationToken);
        if (tenantId is null) return Result<Guid>.Failure(_invalid);
        var identity = new AuthenticationIdentity(tenantId.Value, null);
        await using var context = new CmsDbContext(databaseOptions, identity);
        var unit = new UnitOfWork(context, identity, identity, clock, ids);
        return await unit.ExecuteAsync(async () =>
        {
            var normalized = email.Trim().ToUpperInvariant();
            if (await context.Set<User>().AnyAsync(user => user.Email == normalized, cancellationToken))
                return Result<Guid>.Failure(new Error("Identity.Conflict", "Account cannot be registered."));
            var user = User.Create(ids.NewId(), tenantId.Value, email, _hasher.HashPassword(null!, password));
            if (user.IsFailure) return Result<Guid>.Failure(user.Error);
            context.Add(user.Value);
            return Result<Guid>.Success(user.Value.Id);
        }, cancellationToken);
    }

    // Authentication failures commit counters without converting rejection into a successful login.
    public async Task<Result<TokenPair>> LoginAsync(string tenant, string email, string password, CancellationToken cancellationToken)
    {
        if (!ValidEmail(email) || string.IsNullOrEmpty(password) || password.Length > 128)
            return Result<TokenPair>.Failure(_invalid);
        var tenantId = await ResolveTenantAsync(tenant, cancellationToken);
        if (tenantId is null) return Result<TokenPair>.Failure(_invalid);
        var identity = new AuthenticationIdentity(tenantId.Value, null);
        await using var context = new CmsDbContext(databaseOptions, identity);
        var unit = new UnitOfWork(context, identity, identity, clock, ids);
        TokenPair? pair = null;
        var result = await unit.ExecuteAsync(async () =>
        {
            var normalized = email.Trim().ToUpperInvariant();
            // Hold an update lock until commit so concurrent failures cannot lose lockout increments.
            var user = await context.Set<User>().FromSqlInterpolated($"SELECT * FROM idn_users WITH (UPDLOCK, ROWLOCK) WHERE tenant_id={tenantId.Value} AND email={normalized}")
                .SingleOrDefaultAsync(cancellationToken);
            if (user is null)
            {
                _ = _hasher.HashPassword(null!, password);
                return Result<Guid>.Success(tenantId.Value);
            }
            if (user.LockedUntilUtc > clock.UtcNow) return Result<Guid>.Success(user.Id);
            var verification = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
            {
                user.RecordFailure(clock.UtcNow);
                return Result<Guid>.Success(user.Id);
            }
            user.RecordSuccess(verification == PasswordVerificationResult.SuccessRehashNeeded
                ? _hasher.HashPassword(user, password) : user.PasswordHash);
            var sessionId = ids.NewId();
            var refresh = NewRefresh(sessionId);
            var session = LoginSession.Create(sessionId, tenantId.Value, user.Id, Hash(refresh), clock.UtcNow);
            context.Add(session);
            pair = Issue(user, session, refresh);
            return Result<Guid>.Success(session.Id);
        }, cancellationToken);
        return result.IsSuccess && pair is not null ? Result<TokenPair>.Success(pair) : Result<TokenPair>.Failure(_invalid);
    }

    // A replay revokes the family in the same transaction that rejects the refresh request.
    public async Task<Result<TokenPair>> RefreshAsync(string tenant, string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 200 || token.Length < 34 || !Guid.TryParseExact(token[..32], "N", out var sessionId))
            return Result<TokenPair>.Failure(_invalid);
        var tenantId = await ResolveTenantAsync(tenant, cancellationToken);
        if (tenantId is null) return Result<TokenPair>.Failure(_invalid);
        var identity = new AuthenticationIdentity(tenantId.Value, null);
        await using var context = new CmsDbContext(databaseOptions, identity);
        var unit = new UnitOfWork(context, identity, identity, clock, ids);
        TokenPair? pair = null;
        var result = await unit.ExecuteAsync(async () =>
        {
            // Serialize consumption so a concurrent replay observes the committed consumed hash.
            var session = await context.Set<LoginSession>().FromSqlInterpolated($"SELECT * FROM idn_sessions WITH (UPDLOCK, ROWLOCK) WHERE tenant_id={tenantId.Value} AND id={sessionId}")
                .SingleOrDefaultAsync(cancellationToken);
            if (session is null) return Result<Guid>.Success(sessionId);
            var next = NewRefresh(session.Id);
            if (!session.Rotate(Hash(token), Hash(next), clock.UtcNow)) return Result<Guid>.Success(sessionId);
            var user = await context.Set<User>().SingleAsync(row => row.Id == session.UserId, cancellationToken);
            pair = Issue(user, session, next);
            return Result<Guid>.Success(sessionId);
        }, cancellationToken);
        return result.IsSuccess && pair is not null ? Result<TokenPair>.Success(pair) : Result<TokenPair>.Failure(_invalid);
    }

    // Claim-selected tenant and user must both own the session being revoked.
    public async Task<Result<Guid>> LogoutAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await using var context = new CmsDbContext(databaseOptions, currentTenant);
        var unit = new UnitOfWork(context, currentTenant, currentUser, clock, ids);
        return await unit.ExecuteAsync(async () =>
        {
            var session = await context.Set<LoginSession>().SingleOrDefaultAsync(row => row.Id == sessionId && row.UserId == currentUser.UserId, cancellationToken);
            if (session is null) return Result<Guid>.Failure(_invalid);
            session.Revoke(clock.UtcNow);
            return Result<Guid>.Success(sessionId);
        }, cancellationToken);
    }

    // SQL is the revocation authority, so Redis outages cannot resurrect logged-out sessions.
    public async Task<bool> IsSessionActiveAsync(Guid tenantId, Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        await using var context = new CmsDbContext(databaseOptions, new AuthenticationIdentity(tenantId, userId));
        return await context.Set<LoginSession>().AsNoTracking().AnyAsync(row => row.Id == sessionId && row.UserId == userId
            && row.RevokedAtUtc == null && row.ExpiresAtUtc > clock.UtcNow, cancellationToken);
    }

    private async Task<Guid?> ResolveTenantAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 63) return null;
        await using var context = new CmsDbContext(databaseOptions, new AuthenticationIdentity(null, null));
        var normalized = slug.Trim().ToLowerInvariant();
        return await context.Tenants.IgnoreQueryFilters().AsNoTracking().Where(row => row.Slug == normalized)
            .Select(row => (Guid?)row.Id).SingleOrDefaultAsync(cancellationToken);
    }

    // Rebuild permissions from current tenant-owned roles rather than trusting stale token claims.
    public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken)
    {
        await using var context = new CmsDbContext(databaseOptions, new AuthenticationIdentity(tenantId, userId));
        var user = await context.Set<User>().AsNoTracking().SingleOrDefaultAsync(row => row.Id == userId, cancellationToken);
        if (user is null) return [];
        if (user.IsAdministrator) return CMS.Domain.Identity.Roles.Role.KnownPermissions;
        var ids = System.Text.Json.JsonSerializer.Deserialize<Guid[]>(user.RoleIdsJson)!;
        var permissions = await context.Set<CMS.Domain.Identity.Roles.Role>().AsNoTracking()
            .Where(role => ids.Contains(role.Id)).Select(role => role.PermissionsJson).ToListAsync(cancellationToken);
        return permissions.SelectMany(json => System.Text.Json.JsonSerializer.Deserialize<string[]>(json)!).Distinct().ToArray();
    }

    private TokenPair Issue(User user, LoginSession session, string refresh)
    {
        var now = clock.UtcNow;
        var expires = now.AddMinutes(15);
        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()), new("tenant_id", user.TenantId.ToString()), new("sid", session.Id.ToString()),
            new("jti", Guid.NewGuid().ToString("N")), new("role", user.IsAdministrator ? "administrator" : "member"),
            new("iat", now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };
        if (user.IsAdministrator) claims.Add(new Claim("permissions", "platform.manage"));
        var token = new JwtSecurityToken(jwt.Value.Issuer, jwt.Value.Audience, claims, now.UtcDateTime, expires.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new TokenPair(new JwtSecurityTokenHandler().WriteToken(token), refresh, expires);
    }

    private static string NewRefresh(Guid id) => id.ToString("N") + "." + Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static bool ValidPassword(string value) => !string.IsNullOrWhiteSpace(value) && value.Length is >= 12 and <= 128;
    private static bool ValidEmail(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 254
        && System.Net.Mail.MailAddress.TryCreate(value, out var parsed) && parsed.Address.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase);

    private sealed record AuthenticationIdentity(Guid? TenantId, Guid? UserId) : ICurrentTenant, ICurrentUser;
}
