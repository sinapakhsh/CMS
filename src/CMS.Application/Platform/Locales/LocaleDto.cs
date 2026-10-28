namespace CMS.Application.Platform.Locales;

// Read models do not expose tracked entities or mutable persistence tokens.
public sealed record LocaleDto(Guid Id, string Code, string DisplayName, bool IsRightToLeft);
