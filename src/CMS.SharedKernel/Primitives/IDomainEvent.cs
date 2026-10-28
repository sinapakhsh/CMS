namespace CMS.SharedKernel.Primitives;

// Domain events carry no messaging-framework dependencies into the domain model.
public interface IDomainEvent
{
    // Stable event identifiers support deduplication during outbox delivery.
    Guid EventId { get; }

    // The producing aggregate supplies time so tests and transactions stay deterministic.
    DateTimeOffset OccurredAtUtc { get; }
}
