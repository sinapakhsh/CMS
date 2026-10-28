using CMS.Application.Common.Abstractions;
using CMS.Domain.Platform.Tenants;
using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Platform.Tenants;

// Tenant provisioning uses the explicitly selected administrative tenant identity.
public sealed class CreateTenantHandler(IRepository<Tenant> repository, ICurrentTenant currentTenant, IClock clock)
    : IRequestHandler<CreateTenantCommand, Result<Guid>>
{
    // SaveChanges belongs exclusively to the surrounding unit of work.
    public async Task<Result<Guid>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        if (currentTenant.TenantId is not { } tenantId || tenantId == Guid.Empty)
            return Result<Guid>.Failure(new Error("Tenant.Required", "A trusted tenant scope is required."));
        if (await repository.GetByIdAsync(tenantId, cancellationToken) is not null)
            return Result<Guid>.Failure(new Error("Tenant.Exists", "This tenant already exists."));
        var result = Tenant.Create(tenantId, request.Slug, request.Name, clock.UtcNow);
        if (result.IsFailure) return Result<Guid>.Failure(result.Error);
        repository.Add(result.Value);
        return Result<Guid>.Success(tenantId);
    }
}
