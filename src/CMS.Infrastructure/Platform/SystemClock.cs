using CMS.Application.Common.Abstractions;

namespace CMS.Infrastructure.Platform;

// Centralize wall-clock access so test implementations can control time.
public sealed class SystemClock : IClock
{
    // System timestamps are always emitted with a zero UTC offset.
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
