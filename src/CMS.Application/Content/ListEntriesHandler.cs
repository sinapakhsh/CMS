using MediatR;

namespace CMS.Application.Content;

// Pagination bounds are enforced regardless of the HTTP caller.
public sealed class ListEntriesHandler(IEntryQueries queries) : IRequestHandler<ListEntriesQuery, IReadOnlyList<EntryDto>>
{
    // Avoid unbounded reads or invalid SQL offsets.
    public Task<IReadOnlyList<EntryDto>> Handle(ListEntriesQuery request, CancellationToken cancellationToken) =>
        queries.ListAsync(Math.Clamp(request.Offset, 0, 100000), Math.Clamp(request.Limit, 1, 100), cancellationToken);
}
