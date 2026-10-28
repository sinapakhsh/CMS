using CMS.SharedKernel.Results;

namespace CMS.Application.Identity;

// Anonymous credentials are resolved inside an explicit tenant boundary by infrastructure.
public interface IIdentityService
{
    // Registration creates an unprivileged account in an existing tenant.
    Task<Result<Guid>> RegisterAsync(string tenant, string email, string password, CancellationToken cancellationToken);
    // Authentication persists lockout counters even when credentials are rejected.
    Task<Result<TokenPair>> LoginAsync(string tenant, string email, string password, CancellationToken cancellationToken);
    // Rotation consumes exactly one refresh token and detects family replay.
    Task<Result<TokenPair>> RefreshAsync(string tenant, string token, CancellationToken cancellationToken);
    // Logout revokes the authenticated session, including its access tokens.
    Task<Result<Guid>> LogoutAsync(Guid sessionId, CancellationToken cancellationToken);
    // Access-token validation checks revocation against durable state.
    Task<bool> IsSessionActiveAsync(Guid tenantId, Guid userId, Guid sessionId, CancellationToken cancellationToken);
    // Permissions are read at authentication time so role changes do not wait for JWT expiry.
    Task<IReadOnlyList<string>> GetPermissionsAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken);
}
