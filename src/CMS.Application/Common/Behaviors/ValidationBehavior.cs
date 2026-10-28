using CMS.Application.Common.Abstractions;
using CMS.SharedKernel.Results;
using FluentValidation;
using MediatR;

namespace CMS.Application.Common.Behaviors;

// Validation failures must not open a database transaction.
public sealed class ValidationBehavior<TCommand>(IEnumerable<IValidator<TCommand>> validators)
    : IPipelineBehavior<TCommand, Result<Guid>> where TCommand : ICommand
{
    // Run validators sequentially because they may share scoped services.
    public async Task<Result<Guid>> Handle(TCommand request, RequestHandlerDelegate<Result<Guid>> next, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 128)
            return Result<Guid>.Failure(new Error("Validation.Failed", "An idempotency key of 1 to 128 characters is required."));
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
                return Result<Guid>.Failure(new Error("Validation.Failed", string.Join("; ", result.Errors.Select(error => error.ErrorMessage))));
        }
        return await next();
    }
}
