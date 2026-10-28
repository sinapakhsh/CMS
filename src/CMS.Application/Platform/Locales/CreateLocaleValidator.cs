using CMS.Domain.Platform.Locales;
using FluentValidation;

namespace CMS.Application.Platform.Locales;

// Validate culture names against the domain's supported culture catalogue.
public sealed class CreateLocaleValidator : AbstractValidator<CreateLocaleCommand>
{
    // Application and SQL length limits are intentionally aligned.
    public CreateLocaleValidator()
    {
        RuleFor(command => command.Code).Must(Locale.IsValidCode).WithMessage("A supported locale code is required.");
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
