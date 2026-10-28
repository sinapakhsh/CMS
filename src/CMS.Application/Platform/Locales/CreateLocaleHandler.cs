using System.Globalization;
using CMS.Application.Common.Abstractions;
using CMS.Application.Common.Models;
using CMS.Domain.Platform.Locales;
using CMS.Domain.Platform.Tenants;
using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Platform.Locales;

// Register a locale only inside an existing trusted tenant.
public sealed class CreateLocaleHandler(IRepository<Locale> locales, IRepository<Tenant> tenants,
    ICurrentTenant currentTenant, IIdGenerator ids, IClock clock) : IRequestHandler<CreateLocaleCommand, Result<Guid>>
{
    // Aggregate validation also protects direct calls that bypass the mediator.
    public async Task<Result<Guid>> Handle(CreateLocaleCommand request, CancellationToken cancellationToken)
    {
        if (currentTenant.TenantId is not { } tenantId || tenantId == Guid.Empty)
            return Result<Guid>.Failure(new Error("Tenant.Required", "A trusted tenant scope is required."));
        var result = Locale.Create(ids.NewId(), tenantId, request.Code, request.DisplayName, clock.UtcNow);
        if (result.IsFailure) return Result<Guid>.Failure(result.Error);
        if (await tenants.GetByIdAsync(tenantId, cancellationToken) is null)
            return Result<Guid>.Failure(new Error("Tenant.NotFound", "The tenant does not exist."));
        var code = CultureInfo.GetCultureInfo(request.Code).Name;
        if (await locales.AnyAsync(new Specification<Locale>(locale => locale.Code == code), cancellationToken))
            return Result<Guid>.Failure(new Error("Locale.Exists", "This locale is already registered."));
        locales.Add(result.Value);
        return Result<Guid>.Success(result.Value.Id);
    }
}
