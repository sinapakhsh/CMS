using CMS.Application.Common.Abstractions;
using CMS.Domain.Identity.Roles;
using CMS.Domain.Identity.Users;
using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Identity;

// Both ends of an assignment must be visible through the same tenant filter.
public sealed class AssignRoleHandler(IRepository<User> users, IRepository<Role> roles) : IRequestHandler<AssignRoleCommand, Result<Guid>>
{
    // The repository cannot return an account or role owned by another tenant.
    public async Task<Result<Guid>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        var role = await roles.GetByIdAsync(request.RoleId, cancellationToken);
        if (user is null || role is null) return Result<Guid>.Failure(new Error("Identity.NotFound", "User or role not found."));
        var result = user.AssignRole(role.Id);
        return result.IsSuccess ? Result<Guid>.Success(user.Id) : Result<Guid>.Failure(result.Error);
    }
}
