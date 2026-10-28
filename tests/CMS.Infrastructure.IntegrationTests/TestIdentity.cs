using CMS.Application.Common.Abstractions;

namespace CMS.Infrastructure.IntegrationTests;

// Each test scope explicitly selects its tenant and actor before resolving persistence.
public sealed class TestIdentity : ICurrentTenant, ICurrentUser
{
    // Mutability is test-only and permits exercising mid-scope isolation defenses.
    public Guid? TenantId { get; set; }
    // Audit attribution can be asserted without HTTP authentication.
    public Guid? UserId { get; set; }
}
