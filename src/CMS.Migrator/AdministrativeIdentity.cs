using CMS.Application.Common.Abstractions;

namespace CMS.Migrator;

// The operator explicitly chooses the administrative tenant; HTTP callers cannot use this scope.
internal sealed class AdministrativeIdentity(Guid? tenantId) : ICurrentTenant, ICurrentUser
{
    // Schema migration has no tenant; provisioning requires a nonempty identifier.
    public Guid? TenantId { get; } = tenantId;

    // Operational actions are attributed to the system rather than an invented user.
    public Guid? UserId => null;
}
