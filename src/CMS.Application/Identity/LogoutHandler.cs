using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Identity;

// Infrastructure owns credential hashing and atomic session persistence.
public sealed class LogoutHandler(IIdentityService identity) : IRequestHandler<LogoutCommand, Result<Guid>>
{
    // Cancellation reaches every persistence operation.
    public Task<Result<Guid>> Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        identity.LogoutAsync(request.SessionId, cancellationToken);
}
