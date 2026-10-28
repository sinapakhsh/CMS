using CMS.Infrastructure.Persistence.Write;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Infrastructure.IntegrationTests;

// Upgrade verification uses only the fixture-owned disposable database.
public sealed class MigrationUpgradeTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    // Existing users must receive valid empty role JSON before the new check constraint is installed.
    [Fact]
    public async Task Roles_migration_preserves_preexisting_accounts()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var scope = database.Scope(tenantId);
        var context = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("Content");
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO plt_tenants (id, tenant_id, slug, name) VALUES ({tenantId}, {tenantId}, 'upgrade-test', 'Upgrade test')");
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO idn_users (id, tenant_id, email, password_hash, is_administrator, failed_attempts) VALUES ({userId}, {tenantId}, 'UPGRADE@EXAMPLE.TEST', 'fixture-hash', 0, 0)");
        await migrator.MigrateAsync();
        var user = await context.Set<CMS.Domain.Identity.Users.User>().SingleAsync();
        Assert.Equal(userId, user.Id);
        Assert.Equal("[]", user.RoleIdsJson);
        Assert.False(context.Database.HasPendingModelChanges());
    }
}
