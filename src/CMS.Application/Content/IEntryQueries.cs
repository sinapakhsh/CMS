namespace CMS.Application.Content;

// Read projections never expose a tracked aggregate or unrestricted IQueryable.
public interface IEntryQueries
{
    // Management reads use the authenticated tenant and a bounded page size.
    Task<IReadOnlyList<EntryDto>> ListAsync(int offset, int limit, CancellationToken cancellationToken);
    // Tenant and locale joins constrain anonymous public reads to published content only.
    Task<EntryDto?> GetPublishedAsync(string tenant, string locale, string slug, CancellationToken cancellationToken);
}
