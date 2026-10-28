using CMS.Domain.Identity.Sessions;
using CMS.Domain.Identity.Users;

namespace CMS.Domain.UnitTests;

// Security invariants remain testable without a web host or database.
public sealed class IdentityTests
{
    // Invalid refresh attempts cannot be used to revoke someone else's session.
    [Fact]
    public void Unknown_hash_does_not_revoke_but_reuse_does()
    {
        var now = DateTimeOffset.UtcNow;
        var session = LoginSession.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('a', 64), now);
        Assert.False(session.Rotate(new string('c', 64), new string('d', 64), now));
        Assert.Null(session.RevokedAtUtc);
        Assert.True(session.Rotate(new string('a', 64), new string('b', 64), now));
        Assert.False(session.Rotate(new string('a', 64), new string('c', 64), now));
        Assert.NotNull(session.RevokedAtUtc);
        Assert.False(session.Rotate(new string('b', 64), new string('d', 64), now));
    }

    // An expired lockout restarts the failure counter rather than permanently locking the account.
    [Fact]
    public void Lockout_expires_and_success_resets_failures()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create(Guid.NewGuid(), Guid.NewGuid(), "user@example.test", "hash").Value;
        for (var i = 0; i < 5; i++) user.RecordFailure(now);
        Assert.Equal(now.AddMinutes(15), user.LockedUntilUtc);
        user.RecordFailure(now.AddMinutes(16));
        Assert.Equal(1, user.FailedAttempts);
        Assert.Null(user.LockedUntilUtc);
        user.RecordSuccess("newhash");
        Assert.Equal(0, user.FailedAttempts);
    }
}
