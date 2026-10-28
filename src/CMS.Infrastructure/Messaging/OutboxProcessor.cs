using CMS.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Messaging;

// Delivery happens outside the database transaction after the durable record is visible.
public sealed class OutboxProcessor(OutboxStore store, IOutboxSink sink, IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger)
{
    // Process one bounded delivery so shutdown and competing workers stay responsive.
    public async Task<bool> ProcessOnceAsync(CancellationToken cancellationToken)
    {
        var message = await store.ClaimAsync(cancellationToken);
        if (message is null) return false;
        try
        {
            IDomainEvent domainEvent = EventSerializer.Deserialize(message.EventType, message.Payload);
            if (domainEvent.EventId != message.EventId)
                throw new InvalidOperationException("Event identity does not match the outbox envelope.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.DeliveryTimeoutSeconds));
            await sink.PublishAsync(message.TenantId, domainEvent, timeout.Token);
            if (!await store.CompleteAsync(message, cancellationToken))
                logger.LogWarning("Outbox lease lost before acknowledgement for {EventId}", message.EventId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            await store.FailAsync(message, exception.GetType().Name, cancellationToken);
            logger.LogWarning("Outbox attempt failed for {EventId}: {ErrorCategory}", message.EventId, exception.GetType().Name);
        }
        return true;
    }
}
