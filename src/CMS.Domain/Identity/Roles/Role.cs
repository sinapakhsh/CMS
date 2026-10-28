using System.Text.Json;
using CMS.Domain.Platform;
using CMS.SharedKernel.Results;

namespace CMS.Domain.Identity.Roles;

// Roles store explicit capabilities; arbitrary policy names cannot grant hidden privileges.
public sealed class Role : TenantAggregate
{
    private Role(Guid id, Guid tenantId, string name, string permissionsJson) : base(id, tenantId)
    { Name = name; PermissionsJson = permissionsJson; }
    // Role names are unique within a tenant.
    public string Name { get; private set; }
    // Permissions are bounded strings from the domain catalogue.
    public string PermissionsJson { get; private set; }
    // The catalogue is immutable to callers and shared by role validation and policy registration.
    public static IReadOnlyList<string> KnownPermissions { get; } = Array.AsReadOnly(new[]
    { "platform.manage", "identity.manage", "content.edit", "content.publish", "forms.manage", "store.manage", "media.manage" });
    // A role cannot contain unknown permissions or exceed storage bounds.
    public static Result<Role> Create(Guid id, Guid tenantId, string name, IReadOnlyCollection<string> permissions)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || string.IsNullOrWhiteSpace(name) || name.Length > 100
            || permissions is null || permissions.Count > KnownPermissions.Count || permissions.Any(permission => !KnownPermissions.Contains(permission)))
            return Result<Role>.Failure(new Error("Role.Invalid", "Role name or permissions are invalid."));
        return Result<Role>.Success(new Role(id, tenantId, name.Trim(), JsonSerializer.Serialize(permissions.Distinct().Order().ToArray())));
    }
}
