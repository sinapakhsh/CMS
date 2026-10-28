using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Common.Abstractions;

// Write commands return identifiers and require a caller-controlled replay key.
public interface ICommand : IRequest<Result<Guid>>
{
    // Keys are scoped to the command type and current tenant.
    string IdempotencyKey { get; }
}
