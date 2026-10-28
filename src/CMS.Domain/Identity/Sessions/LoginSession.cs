using System.Text.Json;
using CMS.Domain.Platform;

namespace CMS.Domain.Identity.Sessions;

// A session is a refresh-token family; replay revokes every access token in that family.
public sealed class LoginSession : TenantAggregate
{
    private LoginSession(Guid id, Guid tenantId, Guid userId, string refreshHash, DateTimeOffset expiresAtUtc) : base(id, tenantId)
    {
        UserId = userId;
        RefreshHash = refreshHash;
        ExpiresAtUtc = expiresAtUtc;
    }
    // Ownership is immutable after authentication.
    public Guid UserId { get; private set; }
    // The bearer secret is never persisted in plaintext.
    public string RefreshHash { get; private set; }
    // Consumed hashes distinguish replay from random invalid input without revealing secrets.
    public string ConsumedHashesJson { get; private set; } = "[]";
    // Absolute expiry cannot be extended indefinitely through rotation.
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    // Revocation is durable and checked during access-token authentication.
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    // Session creation accepts only a SHA-256 digest and valid future expiry.
    public static LoginSession Create(Guid id, Guid tenantId, Guid userId, string hash, DateTimeOffset now)
    {
        if (userId == Guid.Empty || hash.Length != 64) throw new ArgumentException("Invalid session identity or digest.");
        return new LoginSession(id, tenantId, userId, hash, now.AddDays(30));
    }
    // Returning false still requires committing revocation when a consumed token was reused.
    public bool Rotate(string presentedHash, string nextHash, DateTimeOffset now)
    {
        if (RevokedAtUtc is not null || ExpiresAtUtc <= now) return false;
        var consumed = JsonSerializer.Deserialize<List<string>>(ConsumedHashesJson)!;
        if (consumed.Contains(presentedHash, StringComparer.Ordinal))
        {
            Revoke(now);
            return false;
        }
        if (presentedHash != RefreshHash) return false;
        // Bound family history; reauthentication is required after 2,000 rotations.
        if (consumed.Count >= 2000) { Revoke(now); return false; }
        if (nextHash.Length != 64 || nextHash == RefreshHash) throw new ArgumentException("A fresh digest is required.");
        consumed.Add(RefreshHash);
        ConsumedHashesJson = JsonSerializer.Serialize(consumed);
        RefreshHash = nextHash;
        return true;
    }
    // Logout and replay use the same irreversible family revocation.
    public void Revoke(DateTimeOffset now) => RevokedAtUtc ??= now;
}
