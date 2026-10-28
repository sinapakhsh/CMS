using MediatR;

namespace CMS.Application.Content;

// Only published, due content can be returned by the read implementation.
public sealed class PublicEntryHandler(IEntryQueries queries) : IRequestHandler<PublicEntryQuery, EntryDto?>
{
    // Invalid route lengths are rejected before any database call.
    public Task<EntryDto?> Handle(PublicEntryQuery request, CancellationToken cancellationToken) =>
        request.Tenant.Length > 63 || request.Locale.Length > 35 || request.Slug.Length > 200
            ? Task.FromResult<EntryDto?>(null) : queries.GetPublishedAsync(request.Tenant, request.Locale, request.Slug, cancellationToken);
}
