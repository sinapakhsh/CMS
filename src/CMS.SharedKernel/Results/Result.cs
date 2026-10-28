namespace CMS.SharedKernel.Results;

// Model expected failures explicitly while reserving exceptions for programming errors.
public class Result
{
    private protected Result(bool isSuccess, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (isSuccess != (error == Error.None))
        {
            throw new ArgumentException("Success requires no error; failure requires an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    // Success is determined at construction and cannot be changed later.
    public bool IsSuccess { get; }

    // Failure is derived so the two outcome flags cannot diverge.
    public bool IsFailure => !IsSuccess;

    // Successful results expose Error.None for consistent caller handling.
    public Error Error { get; }

    // Commands without a return value can still report successful completion.
    public static Result Success() => new(true, Error.None);

    // Reject empty errors rather than silently constructing an invalid failure.
    public static Result Failure(Error error) => new(false, error);
}
