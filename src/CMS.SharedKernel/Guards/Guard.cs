using System.Runtime.CompilerServices;

namespace CMS.SharedKernel.Guards;

// Guards protect programming invariants; expected business failures use Result instead.
public static class Guard
{
    // Preserve the caller's parameter name when rejecting missing text.
    public static string NotBlank(string? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    // Empty identifiers must not escape into persistence or tenant boundaries.
    public static Guid NotEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("An identifier cannot be empty.", parameterName);
        }

        return value;
    }

    // Persist UTC timestamps so consumers do not infer a server-local time zone.
    public static DateTimeOffset Utc(DateTimeOffset value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("A timestamp must have a zero UTC offset.", parameterName);
        }

        return value;
    }
}
