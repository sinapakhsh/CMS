using CMS.Domain.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Write.Configurations;

// Apply the same immutable tenant and concurrency rules to every aggregate.
public abstract class BaseConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : TenantAggregate
{
    // Aggregate tables are independent roots; the shared base is not an EF inheritance hierarchy.
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasBaseType((Type?)null);
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entity => entity.TenantId).HasColumnName("tenant_id").IsConcurrencyToken();
        builder.Property(entity => entity.RowVersion).HasColumnName("row_version").IsRowVersion();
        builder.Ignore(entity => entity.DomainEvents);
    }
}
