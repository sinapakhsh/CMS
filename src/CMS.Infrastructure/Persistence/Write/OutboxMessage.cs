namespace CMS.Infrastructure.Persistence.Write;

// Durable delivery metadata is persisted atomically with the aggregate change.
public sealed class OutboxMessage
{
    // The clustered key is independent of the event's logical identifier.
    public Guid Id { get; set; }
    // Logical event identity remains stable through retries and lease changes.
    public Guid EventId { get; set; }
    // Delivery retains the tenant for downstream authorization and deduplication.
    public Guid TenantId { get; set; }
    // Explicit versioned event names avoid deserializing arbitrary CLR types.
    public string EventType { get; set; } = string.Empty;
    // JSON contains only the approved domain event contract.
    public string Payload { get; set; } = string.Empty;
    // Event time comes from the producing aggregate.
    public DateTimeOffset OccurredAtUtc { get; set; }
    // Retried work becomes eligible only after its scheduled backoff.
    public DateTimeOffset NextAttemptAtUtc { get; set; }
    // A fresh token fences each worker's ownership of the delivery.
    public Guid? LockToken { get; set; }
    // Expired leases allow recovery after a crashed worker.
    public DateTimeOffset? LockedUntilUtc { get; set; }
    // Attempts increase when a worker claims the message.
    public int Attempts { get; set; }
    // Acknowledgement is recorded only after the sink succeeds.
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    // Exhausted messages remain available for explicit operational recovery.
    public DateTimeOffset? DeadLetterAtUtc { get; set; }
    // Persist an error category rather than potentially sensitive exception text.
    public string? LastError { get; set; }
}
