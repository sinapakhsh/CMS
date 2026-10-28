using FluentValidation;

namespace CMS.Application.Platform.Tenants;

// Bounds match database constraints before any SQL work is attempted.
public sealed class CreateTenantValidator : AbstractValidator<CreateTenantCommand>
{
    // The accepted slug format remains stable across all callers.
    public CreateTenantValidator()
    {
        RuleFor(command => command.Slug).NotEmpty().MaximumLength(63).Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
