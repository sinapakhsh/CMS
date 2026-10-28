namespace CMS.SharedKernel.Results;

// Stable codes allow callers to translate expected failures without parsing messages.
public sealed record Error
{
    // Empty errors are reserved for successful results.
    public static readonly Error None = new();

    // Prevent incomplete failure descriptions from entering the application boundary.
    public Error(string code, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Code = code;
        Description = description;
    }

    private Error()
    {
        Code = string.Empty;
        Description = string.Empty;
    }

    // Callers branch on codes instead of language-dependent messages.
    public string Code { get; }

    // A description explains the failure without coupling the kernel to localization.
    public string Description { get; }
}
