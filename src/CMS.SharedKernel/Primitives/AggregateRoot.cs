using System.Collections.ObjectModel;

namespace CMS.SharedKernel.Primitives;

// Keep events with the aggregate until the persistence boundary safely records them.
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly ReadOnlyCollection<IDomainEvent> _eventView;

    // Expose a read-only wrapper so callers cannot mutate the pending event list.
    protected AggregateRoot(TId id) : base(id)
    {
        _eventView = _domainEvents.AsReadOnly();
    }

    // The outbox can inspect events without taking ownership before persistence succeeds.
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _eventView;

    // Persistence calls this only after pending events have been safely recorded.
    public void ClearDomainEvents() => _domainEvents.Clear();

    // Enforce event metadata before it can become part of a durable outbox record.
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        if (domainEvent.EventId == Guid.Empty)
        {
            throw new ArgumentException("An event must have an identifier.", nameof(domainEvent));
        }

        if (domainEvent.OccurredAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Event timestamps must be UTC.", nameof(domainEvent));
        }

        if (_domainEvents.Any(pending => pending.EventId == domainEvent.EventId))
        {
            throw new ArgumentException("An event identifier cannot be queued twice.", nameof(domainEvent));
        }

        _domainEvents.Add(domainEvent);
    }
}
