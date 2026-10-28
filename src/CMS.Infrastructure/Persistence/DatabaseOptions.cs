namespace CMS.Infrastructure.Persistence;

// Required connection configuration is validated before the application serves requests.
public sealed class DatabaseOptions
{
    // Populate through environment variables or user secrets, never source-controlled settings.
    public string ConnectionString { get; set; } = string.Empty;
}
