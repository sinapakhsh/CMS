using CMS.Domain.Platform.Locales;
using CMS.Domain.Platform.Tenants;
using CMS.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;

namespace CMS.Infrastructure.Messaging;

// Phase-one events have an explicit structured-log consumer; later integrations add their own contracts.
public sealed class PlatformEventSink(ILogger<PlatformEventSink> logger) : IOutboxSink
{
    // Never mark an unknown event as delivered without a registered consumer.
    public Task PublishAsync(Guid tenantId, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payloadTenant = domainEvent switch
        {
            TenantCreated created => created.TenantId,
            LocaleCreated created => created.TenantId,
            _ => throw new InvalidOperationException("No consumer is registered for the event.")
        };
        if (payloadTenant != tenantId || domainEvent.EventId == Guid.Empty)
            throw new InvalidOperationException("Outbox event metadata does not match its envelope.");
        logger.LogInformation("Platform event {EventType} {EventId} delivered for tenant {TenantId}",
            EventSerializer.TypeName(domainEvent), domainEvent.EventId, tenantId);
        return Task.CompletedTask;
    }
}
