using CMS.SharedKernel.Primitives;

namespace CMS.Domain.Platform.Tenants;

// Use a minimal payload so domain notifications do not copy personal data.
public sealed record TenantCreated(Guid EventId, DateTimeOffset OccurredAtUtc, Guid TenantId) : IDomainEvent;
