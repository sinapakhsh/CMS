using CMS.Application.Common.Abstractions;
using CMS.Infrastructure.Persistence.Write;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CMS.Infrastructure.Persistence;

// Design-time commands require an explicit connection string and never run migrations automatically.
public sealed class CmsDbContextFactory : IDesignTimeDbContextFactory<CmsDbContext>
{
    // Model generation does not open a connection; database updates remain an explicit command.
    public CmsDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Cms");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set ConnectionStrings__Cms for EF tooling.");
        return new CmsDbContext(new DbContextOptionsBuilder<CmsDbContext>().UseSqlServer(connection).Options, new DesignTenant());
    }

    private sealed class DesignTenant : ICurrentTenant
    {
        // Schema operations never need unrestricted application data access.
        public Guid? TenantId => null;
    }
}
