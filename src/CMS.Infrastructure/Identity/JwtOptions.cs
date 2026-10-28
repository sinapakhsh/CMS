namespace CMS.Infrastructure.Identity;

// Deployment supplies signing material through environment variables or user secrets.
public sealed class JwtOptions
{
    // An explicit issuer prevents accepting tokens from unrelated deployments.
    public string Issuer { get; set; } = "cms";
    // The API accepts only tokens issued for its configured audience.
    public string Audience { get; set; } = "cms-api";
    // A random secret of at least 32 UTF-8 bytes is required at startup.
    public string SigningKey { get; set; } = string.Empty;
}
