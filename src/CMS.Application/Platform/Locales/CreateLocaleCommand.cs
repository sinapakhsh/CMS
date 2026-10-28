using CMS.Application.Common.Abstractions;

namespace CMS.Application.Platform.Locales;

// The tenant is resolved from trusted context rather than accepted in this payload.
public sealed record CreateLocaleCommand(string Code, string DisplayName, string IdempotencyKey) : ICommand;
