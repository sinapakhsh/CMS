using System.Text.Json;
using CMS.Domain.Platform.Locales;
using CMS.Domain.Platform.Tenants;
using CMS.SharedKernel.Primitives;

namespace CMS.Infrastructure.Messaging;

// An explicit allowlist prevents database payloads from selecting arbitrary CLR types.
public static class EventSerializer
{
    // Event names are versioned independently of namespaces and assembly versions.
    public static string TypeName(IDomainEvent domainEvent) => domainEvent switch
    {
        TenantCreated => "platform.tenant.created.v1",
        LocaleCreated => "platform.locale.created.v1",
        _ => throw new InvalidOperationException("The domain event has no registered outbox contract.")
    };

    // Serialize only the concrete approved event contract.
    public static string Serialize(IDomainEvent domainEvent)
    {
        _ = TypeName(domainEvent);
        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
    }

    // Unknown or malformed events remain failures instead of silently disappearing.
    public static IDomainEvent Deserialize(string eventType, string payload) => eventType switch
    {
        "platform.tenant.created.v1" => JsonSerializer.Deserialize<TenantCreated>(payload)
            ?? throw new InvalidOperationException("Missing tenant event."),
        "platform.locale.created.v1" => JsonSerializer.Deserialize<LocaleCreated>(payload)
            ?? throw new InvalidOperationException("Missing locale event."),
        _ => throw new InvalidOperationException("Unknown outbox contract.")
    };
}
