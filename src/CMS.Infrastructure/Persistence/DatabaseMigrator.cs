using CMS.Infrastructure.Persistence.Write;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Persistence;

// Only the dedicated administrative executable invokes schema changes.
public sealed class DatabaseMigrator(CmsDbContext context)
{
    // Honor cancellation so a deployment can stop without starting another migration.
    public Task MigrateAsync(CancellationToken cancellationToken) => context.Database.MigrateAsync(cancellationToken);
}
