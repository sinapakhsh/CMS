using CMS.SharedKernel.Primitives;

namespace CMS.SharedKernel.UnitTests;

// Protect the event hand-off contract used by later transaction and outbox code.
public sealed class AggregateRootTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);

    // Invalid identity is rejected before an aggregate can collect events.
    [Fact]
    public void Aggregate_requires_identity()
    {
        Assert.Throws<ArgumentException>(() => new SampleAggregate(Guid.Empty));
        var id = Guid.NewGuid();
        Assert.Equal(id, new SampleAggregate(id).Id);
    }

    // Reading events must neither consume them nor allow external mutation.
    [Fact]
    public void Events_remain_ordered_and_read_only_until_explicitly_cleared()
    {
        var aggregate = new SampleAggregate(Guid.NewGuid());
        var first = new SampleEvent(Guid.NewGuid(), _now);
        var second = new SampleEvent(Guid.NewGuid(), _now.AddSeconds(1));
        aggregate.Raise(first);
        aggregate.Raise(second);

        Assert.Equal(new IDomainEvent[] { first, second }, aggregate.DomainEvents);
        Assert.Equal(2, aggregate.DomainEvents.Count);
        var collection = Assert.IsAssignableFrom<ICollection<IDomainEvent>>(aggregate.DomainEvents);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Add(first));

        aggregate.ClearDomainEvents();
        Assert.Empty(aggregate.DomainEvents);
        aggregate.ClearDomainEvents();
        aggregate.Raise(first);
        Assert.Single(aggregate.DomainEvents);
    }

    // Reject invalid events without corrupting the pending event collection.
    [Fact]
    public void Invalid_event_metadata_does_not_add_an_event()
    {
        var aggregate = new SampleAggregate(Guid.NewGuid());
        Assert.Throws<ArgumentNullException>(() => aggregate.Raise(null!));
        Assert.Throws<ArgumentException>(() => aggregate.Raise(new SampleEvent(Guid.Empty, _now)));
        Assert.Throws<ArgumentException>(() => aggregate.Raise(
            new SampleEvent(Guid.NewGuid(), _now.ToOffset(TimeSpan.FromHours(3.5)))));
        Assert.Empty(aggregate.DomainEvents);
    }

    // Duplicate event identifiers would make downstream deduplication ambiguous.
    [Fact]
    public void Duplicate_event_ids_are_rejected()
    {
        var aggregate = new SampleAggregate(Guid.NewGuid());
        var id = Guid.NewGuid();
        aggregate.Raise(new SampleEvent(id, _now));
        Assert.Throws<ArgumentException>(() => aggregate.Raise(new SampleEvent(id, _now.AddSeconds(1))));
        Assert.Single(aggregate.DomainEvents);
    }

    private sealed class SampleAggregate(Guid id) : AggregateRoot<Guid>(id)
    {
        // Let tests exercise the protected aggregate API without persistence.
        public void Raise(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record SampleEvent(Guid EventId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
}
