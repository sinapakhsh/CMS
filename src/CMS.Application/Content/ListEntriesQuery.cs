using MediatR;

namespace CMS.Application.Content;

// Offset pagination is limited to the management API.
public sealed record ListEntriesQuery(int Offset = 0, int Limit = 50) : IRequest<IReadOnlyList<EntryDto>>;
