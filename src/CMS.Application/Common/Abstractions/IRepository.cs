using CMS.Application.Common.Models;
using CMS.Domain.Platform;

namespace CMS.Application.Common.Abstractions;

// Only aggregate roots are exposed through the write repository.
public interface IRepository<TEntity> where TEntity : TenantAggregate
{
    // Lookup always applies the current tenant filter, including for tracked entities.
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Specifications keep EF query types outside application handlers.
    Task<bool> AnyAsync(Specification<TEntity> specification, CancellationToken cancellationToken);

    // New aggregates remain pending until the unit of work commits.
    void Add(TEntity entity);

    // Updates are restricted to entities loaded in this scoped unit of work.
    void Update(TEntity entity);

    // Hard deletion is explicit; business soft-delete behavior belongs to the aggregate.
    void Remove(TEntity entity);
}
