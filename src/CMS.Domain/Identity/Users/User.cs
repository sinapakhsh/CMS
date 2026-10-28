using CMS.Domain.Platform;
using CMS.SharedKernel.Results;

namespace CMS.Domain.Identity.Users;

// Authentication state is tenant-owned and protected by optimistic concurrency.
public sealed class User : TenantAggregate
{
    private User(Guid id, Guid tenantId, string email, string passwordHash) : base(id, tenantId)
    {
        Email = email;
        PasswordHash = passwordHash;
    }
    // Normalization makes account identity independent of letter casing.
    public string Email { get; private set; }
    // Only the password hasher's encoded output is persisted.
    public string PasswordHash { get; private set; }
    // Public registration never grants administrative privileges.
    public bool IsAdministrator { get; private set; }
    // Role identities are assigned only after tenant-scoped lookup by an authorized handler.
    public string RoleIdsJson { get; private set; } = "[]";
    // Assignment is idempotent and bounded to prevent an unbounded authorization payload.
    public Result AssignRole(Guid roleId)
    {
        var roles = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(RoleIdsJson)!;
        if (roleId == Guid.Empty || roles.Count >= 32 && !roles.Contains(roleId))
            return Result.Failure(new Error("User.InvalidRole", "A valid role is required and at most 32 roles are allowed."));
        if (!roles.Contains(roleId)) roles.Add(roleId);
        RoleIdsJson = System.Text.Json.JsonSerializer.Serialize(roles);
        return Result.Success();
    }
    // Failed login counts survive process restarts.
    public int FailedAttempts { get; private set; }
    // Lockouts expire using server UTC time.
    public DateTimeOffset? LockedUntilUtc { get; private set; }
    // The domain bounds values even when invoked outside the API.
    public static Result<User> Create(Guid id, Guid tenantId, string email, string passwordHash)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || string.IsNullOrWhiteSpace(email)
            || email.Length > 254 || !email.Contains('@') || string.IsNullOrWhiteSpace(passwordHash))
            return Result<User>.Failure(new Error("User.Invalid", "Valid identity, email and password hash are required."));
        return Result<User>.Success(new User(id, tenantId, email.Trim().ToUpperInvariant(), passwordHash));
    }
    // Provisioning invokes this only through a trusted local administrative command.
    public void GrantAdministration() => IsAdministrator = true;
    // Five consecutive failures impose a bounded lockout without storing supplied passwords.
    public void RecordFailure(DateTimeOffset now)
    {
        if (LockedUntilUtc > now) return;
        if (LockedUntilUtc is not null) FailedAttempts = 0;
        LockedUntilUtc = null;
        if (++FailedAttempts >= 5) LockedUntilUtc = now.AddMinutes(15);
    }
    // Successful authentication clears counters and supports password hash upgrades.
    public void RecordSuccess(string hash)
    {
        FailedAttempts = 0;
        LockedUntilUtc = null;
        PasswordHash = hash;
    }
}
