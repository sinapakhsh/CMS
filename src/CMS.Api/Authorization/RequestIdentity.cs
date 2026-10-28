using CMS.Application.Common.Abstractions;

namespace CMS.Api.Authorization;

// Read only authenticated claims; arbitrary headers must never select a tenant.
public sealed class RequestIdentity(IHttpContextAccessor accessor) : ICurrentTenant, ICurrentUser
{
    // Phase two will populate this through the official JWT authentication middleware.
    public Guid? TenantId => ReadGuid("tenant_id");

    // Actor identity is recorded separately from the tenant boundary.
    public Guid? UserId => ReadGuid("sub");

    private Guid? ReadGuid(string claim)
    {
        var user = accessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true && Guid.TryParse(user.FindFirst(claim)?.Value, out var id)
            && id != Guid.Empty ? id : null;
    }
}
