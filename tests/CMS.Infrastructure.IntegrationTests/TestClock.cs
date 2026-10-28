using CMS.Application.Common.Abstractions;

namespace CMS.Infrastructure.IntegrationTests;

// Advance time without sleeping when testing leases and replay expiry.
public sealed class TestClock : IClock
{
    // SQL datetimeoffset can represent this deterministic baseline exactly.
    public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);
}
