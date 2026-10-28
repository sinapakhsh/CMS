using MediatR;

namespace CMS.Application.Platform.Locales;

// Keep query orchestration independent of the Dapper implementation.
public sealed class ListLocalesHandler(ILocaleQueries queries) : IRequestHandler<ListLocalesQuery, IReadOnlyList<LocaleDto>>
{
    // Cancellation flows unchanged to the database query.
    public Task<IReadOnlyList<LocaleDto>> Handle(ListLocalesQuery request, CancellationToken cancellationToken) =>
        queries.ListAsync(cancellationToken);
}
