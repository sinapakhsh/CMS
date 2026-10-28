using MediatR;

namespace CMS.Application.Platform.Locales;

// Read requests cannot enter the command transaction pipeline.
public sealed record ListLocalesQuery : IRequest<IReadOnlyList<LocaleDto>>;
