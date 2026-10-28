namespace CMS.Infrastructure.Persistence.Write;

// Store only metadata so auditing cannot accidentally retain passwords or private payloads.
public sealed class AuditRecord
{
    // Sequential identifiers keep insert locality predictable.
    public Guid Id { get; set; }
    // Audit visibility follows the same tenant boundary as business rows.
    public Guid TenantId { get; set; }
    // Administrative operations may intentionally omit a human actor.
    public Guid? ActorId { get; set; }
    // Entity identifiers support investigation without copying entity contents.
    public Guid EntityId { get; set; }
    // Stable CLR names identify the aggregate category.
    public string EntityType { get; set; } = string.Empty;
    // Record the persisted operation, not a client-supplied description.
    public string Operation { get; set; } = string.Empty;
    // Commit metadata uses the injected UTC clock.
    public DateTimeOffset OccurredAtUtc { get; set; }
}
