using CMS.SharedKernel.Primitives;

namespace CMS.Infrastructure.Messaging;

// Sinks must tolerate repeated event IDs because delivery is at least once.
public interface IOutboxSink
{
    // The tenant and event are validated before external delivery is attempted.
    Task PublishAsync(Guid tenantId, IDomainEvent domainEvent, CancellationToken cancellationToken);
}
