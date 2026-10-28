using System.Text.RegularExpressions;
using CMS.Application;
using CMS.Application.Common.Abstractions;
using CMS.Infrastructure;
using CMS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Infrastructure.IntegrationTests;

// Every run owns a new database; an existing user database is never migrated, reset or dropped.
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly string _databaseName = "CMS_PlatformTests_" + Guid.NewGuid().ToString("N");
    private string _masterConnection = string.Empty;

    // Tests share infrastructure registration but use isolated request scopes.
    public ServiceProvider Services { get; private set; } = null!;

    // The generated connection points only to this run's disposable database.
    public string ConnectionString { get; private set; } = string.Empty;

    // One deterministic clock lets lease tests move time without wall-clock delays.
    public TestClock Clock { get; } = new();

    // Use configured SQL Server or Windows LocalDB; never fall back to an in-memory provider.
    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("CMS_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(configured) && !OperatingSystem.IsWindows())
            throw new InvalidOperationException("Set CMS_TEST_SQL_CONNECTION to a disposable SQL Server test instance.");
        var builder = new SqlConnectionStringBuilder(configured ??
            @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=15")
        { InitialCatalog = "master", Pooling = false };
        _masterConnection = builder.ConnectionString;
        await using var connection = new SqlConnection(_masterConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        if (configured is null)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CMS.slnx"))) directory = directory.Parent;
            var dataDirectory = Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException(), "artifacts", "TestDatabases");
            Directory.CreateDirectory(dataDirectory);
            var fileBase = Path.Combine(dataDirectory, _databaseName).Replace("'", "''");
            command.CommandText = $"CREATE DATABASE [{_databaseName}] ON PRIMARY (NAME=N'{_databaseName}', FILENAME=N'{fileBase}.mdf') LOG ON (NAME=N'{_databaseName}_log', FILENAME=N'{fileBase}_log.ldf');";
        }
        else command.CommandText = $"CREATE DATABASE [{_databaseName}];";
        await command.ExecuteNonQueryAsync();
        builder.InitialCatalog = _databaseName;
        ConnectionString = builder.ConnectionString;
        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Cms"] = ConnectionString,
                ["Outbox:Enabled"] = "false"
            }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IClock>(Clock);
            services.AddScoped<TestIdentity>();
            services.AddScoped<ICurrentTenant>(provider => provider.GetRequiredService<TestIdentity>());
            services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<TestIdentity>());
            services.AddCmsApplication();
            services.AddCmsInfrastructure(configuration);
            Services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
            await using var scope = Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync(default);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    // Cleanup verifies the generated database name before issuing a destructive SQL operation.
    public async Task DisposeAsync()
    {
        if (Services is not null) await Services.DisposeAsync();
        if (!Regex.IsMatch(_databaseName, "^CMS_PlatformTests_[0-9a-f]{32}$"))
            throw new InvalidOperationException("Refusing to drop a database outside the test namespace.");
        await using var connection = new SqlConnection(_masterConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID(N'{_databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]; END;";
        await command.ExecuteNonQueryAsync();
    }

    // Tenant and actor are fixed before a scoped DbContext or repository is resolved.
    public AsyncServiceScope Scope(Guid? tenantId, Guid? actorId = null)
    {
        var scope = Services.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<TestIdentity>();
        identity.TenantId = tenantId;
        identity.UserId = actorId;
        return scope;
    }

    // SQL test helpers operate only on the generated disposable database.
    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
