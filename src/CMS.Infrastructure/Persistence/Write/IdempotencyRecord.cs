namespace CMS.Infrastructure.Persistence.Write;

// Only successful command results are cached, inside the same transaction as the mutation.
public sealed class IdempotencyRecord
{
    // Use a storage identifier separate from the caller's key.
    public Guid Id { get; set; }
    // Identical keys in different tenants must never share a result.
    public Guid TenantId { get; set; }
    // Different command types have independent replay scopes.
    public string Operation { get; set; } = string.Empty;
    // The bounded caller key is unique within tenant and operation.
    public string Key { get; set; } = string.Empty;
    // A payload fingerprint detects reuse for a different request.
    public string Fingerprint { get; set; } = string.Empty;
    // Phase-one commands return aggregate identifiers rather than private response bodies.
    public Guid ResultId { get; set; }
    // Expiry bounds the guarantee without relying on a volatile cache.
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
