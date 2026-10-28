using CMS.Application.Common.Abstractions;
using CMS.Application.Common.Models;
using CMS.Domain.Platform;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Persistence.Write.Repositories;

// Queries always use filters; Find would return an already tracked entity without rechecking SQL.
public sealed class Repository<TEntity>(CmsDbContext context, ICurrentTenant tenant) : IRepository<TEntity> where TEntity : TenantAggregate
{
    // Query even tracked keys so the tenant predicate remains explicit.
    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        RequireTenant();
        return context.Set<TEntity>().SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    // Specifications are composed before execution and never return an IQueryable.
    public Task<bool> AnyAsync(Specification<TEntity> specification, CancellationToken cancellationToken)
    {
        RequireTenant();
        return context.Set<TEntity>().AnyAsync(specification.Criteria, cancellationToken);
    }

    // Tenant ownership is checked before tracking the aggregate.
    public void Add(TEntity entity)
    {
        CheckTenant(entity);
        context.Set<TEntity>().Add(entity);
    }

    // Detached updates are prohibited to avoid accepting fabricated original concurrency values.
    public void Update(TEntity entity)
    {
        CheckTracked(entity);
        context.ChangeTracker.DetectChanges();
    }

    // A caller must first load the aggregate inside the tenant scope.
    public void Remove(TEntity entity)
    {
        CheckTracked(entity);
        context.Set<TEntity>().Remove(entity);
    }

    private void CheckTracked(TEntity entity)
    {
        CheckTenant(entity);
        if (context.Entry(entity).State == EntityState.Detached)
            throw new InvalidOperationException("Load the aggregate before modifying it.");
    }

    private void CheckTenant(TEntity entity)
    {
        RequireTenant();
        if (entity.TenantId != tenant.TenantId) throw new InvalidOperationException("Cross-tenant writes are prohibited.");
    }

    private void RequireTenant()
    {
        if (tenant.TenantId is null || tenant.TenantId == Guid.Empty)
            throw new InvalidOperationException("A trusted tenant scope is required.");
    }
}
