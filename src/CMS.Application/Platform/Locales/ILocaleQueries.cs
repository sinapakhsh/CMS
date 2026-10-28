namespace CMS.Application.Platform.Locales;

// Database-specific list queries stay behind an application contract.
public interface ILocaleQueries
{
    // Bound result sizes and scope all reads to the trusted tenant.
    Task<IReadOnlyList<LocaleDto>> ListAsync(CancellationToken cancellationToken);
}
