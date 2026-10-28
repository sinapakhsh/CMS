using CMS.SharedKernel.Primitives;

namespace CMS.Domain.Platform.Locales;

// Stable event payloads can be stored independently of EF entity representations.
public sealed record LocaleCreated(Guid EventId, DateTimeOffset OccurredAtUtc, Guid TenantId, Guid LocaleId, string Code) : IDomainEvent;
