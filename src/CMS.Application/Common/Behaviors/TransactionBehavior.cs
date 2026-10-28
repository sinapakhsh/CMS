using CMS.Application.Common.Abstractions;
using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Common.Behaviors;

// Handlers cannot persist part of a command independently of its outcome.
public sealed class TransactionBehavior<TCommand>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TCommand, Result<Guid>> where TCommand : ICommand
{
    // The outer idempotency scope owns the commit when one already exists.
    public Task<Result<Guid>> Handle(TCommand request, RequestHandlerDelegate<Result<Guid>> next, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(() => next(), cancellationToken);
}
