using CMS.SharedKernel.Guards;
using CMS.SharedKernel.Primitives;

namespace CMS.Domain.Platform;

// Tenant identity is immutable and also participates in database concurrency checks.
public abstract class TenantAggregate : AggregateRoot<Guid>
{
    // Callers must supply the trusted tenant rather than infer it from request payloads.
    protected TenantAggregate(Guid id, Guid tenantId) : base(id) => TenantId = Guard.NotEmpty(tenantId);

    // Every persisted aggregate belongs to exactly one tenant.
    public Guid TenantId { get; private set; }

    // SQL Server supplies the concurrency token after every successful write.
    public byte[] RowVersion { get; private set; } = [];
}
