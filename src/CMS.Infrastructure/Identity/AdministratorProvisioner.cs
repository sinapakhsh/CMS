using CMS.Application.Common.Abstractions;
using CMS.Domain.Identity.Users;
using CMS.Infrastructure.Persistence.Write;
using CMS.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Identity;

// Administrative access can only be bootstrapped through the local operator tool.
public sealed class AdministratorProvisioner(CmsDbContext context, ICurrentTenant tenant, IUnitOfWork unitOfWork, IIdGenerator ids)
{
    // Passwords arrive through environment configuration, never command-line arguments or logs.
    public Task<Result<Guid>> CreateAsync(string email, string password, CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async () =>
    {
        if (tenant.TenantId is not { } tenantId || !await context.Tenants.AnyAsync(row => row.Id == tenantId, cancellationToken))
            return Result<Guid>.Failure(new Error("Tenant.NotFound", "Provision the tenant first."));
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 128
            || !System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email || email.Length > 254)
            return Result<Guid>.Failure(new Error("Identity.Invalid", "A valid email and 12 to 128 character password are required."));
        var normalized = email.ToUpperInvariant();
        if (await context.Set<User>().AnyAsync(row => row.Email == normalized, cancellationToken))
            return Result<Guid>.Failure(new Error("Identity.Conflict", "The account already exists; implicit elevation is prohibited."));
        var user = User.Create(ids.NewId(), tenantId, email, new PasswordHasher<User>().HashPassword(null!, password)).Value;
        user.GrantAdministration();
        context.Add(user);
        return Result<Guid>.Success(user.Id);
    }, cancellationToken);
}
