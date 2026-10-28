using CMS.Domain.Identity.Users;
using CMS.Domain.Platform.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Write.Configurations;

// Database uniqueness closes concurrent registration races.
public sealed class UserConfiguration : BaseConfiguration<User>
{
    // No credentials or user data can cascade across tenant boundaries.
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);
        builder.ToTable("idn_users", table => table.HasCheckConstraint("ck_user_roles", "ISJSON([role_ids_json])=1"));
        builder.Property(user => user.RoleIdsJson).HasMaxLength(2000);
        builder.Property(user => user.Email).HasMaxLength(254);
        builder.Property(user => user.PasswordHash).HasMaxLength(500);
        builder.HasIndex(user => new { user.TenantId, user.Email }).IsUnique();
        builder.HasAlternateKey(user => new { user.TenantId, user.Id });
        builder.HasOne<Tenant>().WithMany().HasForeignKey(user => user.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
