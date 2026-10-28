using CMS.Application.Common.Abstractions;
using CMS.Domain.Platform.Locales;
using CMS.Domain.Platform.Tenants;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Persistence.Write;

// Tenant filters reference the scoped context so EF model caching cannot reuse another tenant.
public sealed class CmsDbContext(DbContextOptions<CmsDbContext> options, ICurrentTenant tenant) : DbContext(options)
{
    // Missing tenant identity matches no rows rather than granting global visibility.
    public Guid? ActiveTenantId => tenant.TenantId;

    // Root rows use the same tenant filter as subordinate aggregates.
    public DbSet<Tenant> Tenants => Set<Tenant>();
    // Locale writes remain tracked until the unit of work commits.
    public DbSet<Locale> Locales => Set<Locale>();
    // Auditing is metadata-only and transactionally consistent with aggregate changes.
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();
    // Worker access across tenants is implemented explicitly with leased SQL operations.
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    // Replay state is tenant-scoped even when queried by administrative code.
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    // The unit of work is the only supported write boundary for this context.
    public override int SaveChanges() => throw new InvalidOperationException("Use IUnitOfWork to persist changes.");
    // Prevent callers from bypassing audit and outbox through the synchronous overload.
    public override int SaveChanges(bool acceptAllChangesOnSuccess) => throw new InvalidOperationException("Use IUnitOfWork to persist changes.");
    // Prevent callers from bypassing the transactional command pipeline.
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Use IUnitOfWork to persist changes.");
    // Reject even explicit accept-all-changes calls outside the unit of work.
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Use IUnitOfWork to persist changes.");

    internal Task<int> PersistAsync(CancellationToken cancellationToken) => base.SaveChangesAsync(false, cancellationToken);

    // Migrations use exactly the same model as runtime persistence.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CmsDbContext).Assembly);
        modelBuilder.Entity<CMS.Domain.Identity.Roles.Role>().HasQueryFilter(entity => entity.TenantId == ActiveTenantId);
        modelBuilder.Entity<CMS.Domain.Content.ContentTypes.ContentType>().HasQueryFilter(entity => entity.TenantId == ActiveTenantId);
        modelBuilder.Entity<CMS.Domain.Content.Entries.Entry>().HasQueryFilter(entity => entity.TenantId == ActiveTenantId);
        modelBuilder.Entity<CMS.Domain.Content.Entries.EntryRevision>(builder =>
        {
            builder.ToTable("cnt_revisions", table => table.HasCheckConstraint("ck_revision_fields", "ISJSON([fields_json])=1"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.Slug).HasMaxLength(200);
            builder.Property(row => row.Title).HasMaxLength(300);
            builder.HasIndex(row => new { row.TenantId, row.EntryId, row.Version }).IsUnique();
            builder.HasQueryFilter(row => row.TenantId == ActiveTenantId);
        });
        modelBuilder.Entity<CMS.Domain.Content.Entries.EntryRedirect>(builder =>
        {
            builder.ToTable("cnt_redirects");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.Slug).HasMaxLength(200);
            builder.HasIndex(row => new { row.TenantId, row.LocaleId, row.Slug }).IsUnique();
            builder.HasQueryFilter(row => row.TenantId == ActiveTenantId);
        });
        modelBuilder.Entity<CMS.Domain.Identity.Users.User>().HasQueryFilter(entity => entity.TenantId == ActiveTenantId);
        modelBuilder.Entity<CMS.Domain.Identity.Sessions.LoginSession>().HasQueryFilter(entity => entity.TenantId == ActiveTenantId);
        modelBuilder.Entity<Tenant>().HasQueryFilter(entity => entity.TenantId == ActiveTenantId);
        modelBuilder.Entity<Locale>().HasQueryFilter(entity => entity.TenantId == ActiveTenantId);

        modelBuilder.Entity<AuditRecord>(builder =>
        {
            builder.ToTable("plt_audit");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.EntityType).HasMaxLength(100);
            builder.Property(row => row.Operation).HasMaxLength(16);
            builder.HasIndex(row => new { row.TenantId, row.OccurredAtUtc });
            builder.HasQueryFilter(row => row.TenantId == ActiveTenantId);
        });
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("plt_outbox", table => table.HasCheckConstraint("ck_outbox_payload", "ISJSON([payload]) = 1"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.EventType).HasMaxLength(150).IsUnicode(false);
            builder.Property(row => row.Payload).HasColumnType("nvarchar(max)");
            builder.Property(row => row.LastError).HasMaxLength(200);
            builder.HasIndex(row => row.EventId).IsUnique();
            builder.HasIndex(row => new { row.NextAttemptAtUtc, row.LockedUntilUtc })
                .HasFilter("[processed_at_utc] IS NULL AND [dead_letter_at_utc] IS NULL");
            builder.HasQueryFilter(row => row.TenantId == ActiveTenantId);
        });
        modelBuilder.Entity<IdempotencyRecord>(builder =>
        {
            builder.ToTable("plt_idempotency");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.Operation).HasMaxLength(200);
            builder.Property(row => row.Key).HasMaxLength(128);
            builder.Property(row => row.Fingerprint).HasMaxLength(64).IsUnicode(false).IsFixedLength();
            builder.HasIndex(row => new { row.TenantId, row.Operation, row.Key }).IsUnique();
            builder.HasIndex(row => row.ExpiresAtUtc);
            builder.HasQueryFilter(row => row.TenantId == ActiveTenantId);
        });

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(string.Concat(property.Name.Select((character, index) =>
                    index > 0 && char.IsUpper(character) ? "_" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString())));
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                    property.SetColumnType("datetimeoffset(7)");
            }
        }
    }
}
