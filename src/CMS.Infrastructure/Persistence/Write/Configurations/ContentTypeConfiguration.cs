using CMS.Domain.Content.ContentTypes;
using CMS.Domain.Platform.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Write.Configurations;

// A tenant cannot create ambiguous content type keys.
public sealed class ContentTypeConfiguration : BaseConfiguration<ContentType>
{
    // JSON constraints defend storage even against non-application writers.
    public override void Configure(EntityTypeBuilder<ContentType> builder)
    {
        base.Configure(builder);
        builder.ToTable("cnt_types", table => table.HasCheckConstraint("ck_type_fields", "ISJSON([fields_json])=1"));
        builder.Property(row => row.Key).HasMaxLength(63);
        builder.Property(row => row.Name).HasMaxLength(200);
        builder.Property(row => row.FieldsJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(row => new { row.TenantId, row.Key }).IsUnique();
        builder.HasAlternateKey(row => new { row.TenantId, row.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(row => row.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
