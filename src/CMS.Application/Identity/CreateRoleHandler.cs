using CMS.Application.Common.Abstractions;
using CMS.Domain.Identity.Roles;
using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Identity;

// Role validation belongs to the domain and persistence belongs to the pipeline.
public sealed class CreateRoleHandler(IRepository<Role> roles, ICurrentTenant tenant, IIdGenerator ids) : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    // Reject invalid capabilities before adding the role to the unit of work.
    public Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = Role.Create(ids.NewId(), tenant.TenantId ?? Guid.Empty, request.Name, request.Permissions);
        if (role.IsFailure) return Task.FromResult(Result<Guid>.Failure(role.Error));
        roles.Add(role.Value);
        return Task.FromResult(Result<Guid>.Success(role.Value.Id));
    }
}
