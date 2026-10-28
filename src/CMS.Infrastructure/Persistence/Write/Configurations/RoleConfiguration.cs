using CMS.Domain.Identity.Roles;
using CMS.Domain.Platform.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Write.Configurations;

// Role storage cannot contain malformed permission JSON or duplicate tenant names.
public sealed class RoleConfiguration : BaseConfiguration<Role>
{
    // Roles share the tenant and rowversion protections of all mutable aggregates.
    public override void Configure(EntityTypeBuilder<Role> builder)
    {
        base.Configure(builder);
        builder.ToTable("idn_roles", table => table.HasCheckConstraint("ck_role_permissions", "ISJSON([permissions_json])=1"));
        builder.Property(role => role.Name).HasMaxLength(100);
        builder.Property(role => role.PermissionsJson).HasMaxLength(2000);
        builder.HasIndex(role => new { role.TenantId, role.Name }).IsUnique();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(role => role.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
