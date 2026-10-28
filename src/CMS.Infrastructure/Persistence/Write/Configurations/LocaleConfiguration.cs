using CMS.Domain.Platform.Locales;
using CMS.Domain.Platform.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Write.Configurations;

// Locale uniqueness is tenant-scoped and database enforced under concurrent creation.
public sealed class LocaleConfiguration : BaseConfiguration<Locale>
{
    // Restricted deletes keep future content references from silently cascading.
    public override void Configure(EntityTypeBuilder<Locale> builder)
    {
        base.Configure(builder);
        builder.ToTable("plt_locales");
        builder.Property(entity => entity.Code).HasColumnName("code").HasMaxLength(35).IsUnicode(false);
        builder.Property(entity => entity.DisplayName).HasColumnName("display_name").HasMaxLength(100);
        builder.Property(entity => entity.IsRightToLeft).HasColumnName("is_right_to_left");
        builder.HasIndex(entity => new { entity.TenantId, entity.Code }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(entity => entity.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
