using CMS.Domain.Identity.Sessions;
using CMS.Domain.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CMS.Infrastructure.Persistence.Write.Configurations;

// A composite foreign key also enforces session ownership at the database boundary.
public sealed class LoginSessionConfiguration : BaseConfiguration<LoginSession>
{
    // Refresh history contains only hashes and remains valid JSON.
    public override void Configure(EntityTypeBuilder<LoginSession> builder)
    {
        base.Configure(builder);
        builder.ToTable("idn_sessions", table => table.HasCheckConstraint("ck_session_history", "ISJSON([consumed_hashes_json])=1"));
        builder.Property(session => session.RefreshHash).HasMaxLength(64).IsUnicode(false);
        builder.Property(session => session.ConsumedHashesJson).HasColumnType("nvarchar(max)");
        builder.HasOne<User>().WithMany().HasForeignKey(session => new { session.TenantId, session.UserId })
            .HasPrincipalKey(user => new { user.TenantId, user.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
