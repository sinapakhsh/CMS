using CMS.Domain.Platform.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Write.Configurations;

// Tenant root identity is enforced by the database as well as the factory.
public sealed class TenantConfiguration : BaseConfiguration<Tenant>
{
    // Unique slugs prevent ambiguous tenant resolution in later identity flows.
    public override void Configure(EntityTypeBuilder<Tenant> builder)
    {
        base.Configure(builder);
        builder.ToTable("plt_tenants", table => table.HasCheckConstraint("ck_tenant_identity", "[id] = [tenant_id]"));
        builder.Property(entity => entity.Slug).HasColumnName("slug").HasMaxLength(63).IsUnicode(false);
        builder.Property(entity => entity.Name).HasColumnName("name").HasMaxLength(200);
        builder.HasIndex(entity => entity.Slug).IsUnique();
    }
}
