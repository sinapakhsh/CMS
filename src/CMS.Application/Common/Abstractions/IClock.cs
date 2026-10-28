namespace CMS.Application.Common.Abstractions;

// Deterministic time supports reproducible expiry and retry tests.
public interface IClock
{
    // All persistence timestamps use zero-offset UTC.
    DateTimeOffset UtcNow { get; }
}
