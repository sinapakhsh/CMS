using System.Security.Cryptography;
using System.Text.Json;
using CMS.Application.Common.Abstractions;
using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Common.Behaviors;

// Reusing a key with a different command payload must never replay a different operation.
public sealed class IdempotencyBehavior<TCommand>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TCommand, Result<Guid>> where TCommand : ICommand
{
    // Hash typed command bytes; the replay key itself is already part of the scoped identity.
    public Task<Result<Guid>> Handle(TCommand request, RequestHandlerDelegate<Result<Guid>> next, CancellationToken cancellationToken)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(request)));
        return unitOfWork.ExecuteIdempotentAsync(typeof(TCommand).FullName!, request.IdempotencyKey, fingerprint, () => next(), cancellationToken);
    }
}
