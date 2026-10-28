namespace CMS.SharedKernel.Results;

// Keep values inaccessible after failure so missing data cannot be mistaken for success.
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    // Reading a failed result is a caller bug and must be visible immediately.
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    // Null success values are disallowed to distinguish absent data from valid results.
    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value, true, Error.None);
    }

    // Failures do not expose default(T) as meaningful application data.
    public new static Result<T> Failure(Error error) => new(default, false, error);
}
