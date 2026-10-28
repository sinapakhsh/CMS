using CMS.Domain.Content.ContentTypes;
using CMS.Domain.Content.Entries;
using CMS.Domain.Platform.Locales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Write.Configurations;

// Composite keys enforce tenant ownership on every content relationship.
public sealed class EntryConfiguration : BaseConfiguration<Entry>
{
    // Revision children load with the aggregate so snapshots and restore use complete history.
    public override void Configure(EntityTypeBuilder<Entry> builder)
    {
        base.Configure(builder);
        builder.ToTable("cnt_entries", table => table.HasCheckConstraint("ck_entry_fields", "ISJSON([fields_json])=1"));
        builder.Property(row => row.Slug).HasMaxLength(200);
        builder.Property(row => row.Title).HasMaxLength(300);
        builder.Property(row => row.Status).HasMaxLength(16);
        builder.Property(row => row.FieldsJson).HasColumnType("nvarchar(max)");
        builder.HasAlternateKey(row => new { row.TenantId, row.Id });
        builder.HasIndex(row => new { row.TenantId, row.LocaleId, row.Slug }).IsUnique();
        builder.HasOne<ContentType>().WithMany().HasForeignKey(row => new { row.TenantId, row.ContentTypeId })
            .HasPrincipalKey(row => new { row.TenantId, row.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Locale>().WithMany().HasForeignKey(row => new { row.TenantId, row.LocaleId })
            .HasPrincipalKey(row => new { row.TenantId, row.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(row => row.Revisions).WithOne().HasForeignKey(row => new { row.TenantId, row.EntryId })
            .HasPrincipalKey(row => new { row.TenantId, row.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(row => row.Revisions).HasField("_revisions").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        builder.HasMany(row => row.Redirects).WithOne().HasForeignKey(row => new { row.TenantId, row.EntryId })
            .HasPrincipalKey(row => new { row.TenantId, row.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(row => row.Redirects).HasField("_redirects").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}
