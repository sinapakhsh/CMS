using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Identity;

// Infrastructure owns credential hashing and atomic session persistence.
public sealed class LoginHandler(IIdentityService identity) : IRequestHandler<LoginCommand, Result<TokenPair>>
{
    // Cancellation reaches every persistence operation.
    public Task<Result<TokenPair>> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        identity.LoginAsync(request.Tenant, request.Email, request.Password, cancellationToken);
}
