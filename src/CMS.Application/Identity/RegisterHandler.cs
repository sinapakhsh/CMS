using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Identity;

// Infrastructure owns credential hashing and atomic session persistence.
public sealed class RegisterHandler(IIdentityService identity) : IRequestHandler<RegisterCommand, Result<Guid>>
{
    // Cancellation reaches every persistence operation.
    public Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken) =>
        identity.RegisterAsync(request.Tenant, request.Email, request.Password, cancellationToken);
}
