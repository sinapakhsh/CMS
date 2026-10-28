using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Identity;

// Infrastructure owns credential hashing and atomic session persistence.
public sealed class RefreshHandler(IIdentityService identity) : IRequestHandler<RefreshCommand, Result<TokenPair>>
{
    // Cancellation reaches every persistence operation.
    public Task<Result<TokenPair>> Handle(RefreshCommand request, CancellationToken cancellationToken) =>
        identity.RefreshAsync(request.Tenant, request.RefreshToken, cancellationToken);
}
