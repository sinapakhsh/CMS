namespace CMS.Application.Common.Abstractions;

// Tenant context must come from authenticated identity or an explicit trusted administrative scope.
public interface ICurrentTenant
{
    // Null means there is no trusted tenant; persistence must fail closed.
    Guid? TenantId { get; }
}
