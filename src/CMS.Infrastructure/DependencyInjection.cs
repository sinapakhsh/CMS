using CMS.Application.Common.Abstractions;
using CMS.Application.Platform.Locales;
using CMS.Infrastructure.BackgroundJobs;
using CMS.Infrastructure.Health;
using CMS.Infrastructure.Messaging;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.Persistence.Read;
using CMS.Infrastructure.Persistence.Write;
using CMS.Infrastructure.Persistence.Write.Repositories;
using CMS.Infrastructure.Platform;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure;

// The composition root exposes contracts while keeping EF and SQL setup inside infrastructure.
public static class DependencyInjection
{
    // Validate connection and worker options before host startup, even when no command is executed.
    public static IServiceCollection AddCmsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>()
            .Configure(options => options.ConnectionString = configuration.GetConnectionString("Cms") ?? string.Empty)
            .Validate(options => IsValidConnectionString(options.ConnectionString), "ConnectionStrings:Cms must name an application database.")
            .ValidateOnStart();
        services.AddOptions<OutboxOptions>().Bind(configuration.GetSection("Outbox"))
            .Validate(options => options.PollSeconds is >= 1 and <= 300 && options.MaxAttempts is >= 1 and <= 20
                && options.DeliveryTimeoutSeconds >= 1 && options.LeaseSeconds > options.DeliveryTimeoutSeconds
                && options.LeaseSeconds <= 3600, "Outbox timing and retry bounds are invalid.")
            .ValidateOnStart();
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddSingleton<IIdGenerator, SqlServerIdGenerator>();
        services.AddDbContext<CmsDbContext>((provider, options) =>
            options.UseSqlServer(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DatabaseMigrator>();
        services.AddScoped<CMS.Infrastructure.Identity.AdministratorProvisioner>();
        services.AddOptions<CMS.Infrastructure.Identity.JwtOptions>().Bind(configuration.GetSection("Jwt"));
        services.AddScoped<CMS.Application.Identity.IIdentityService, CMS.Infrastructure.Identity.IdentityService>();
        services.AddScoped<ILocaleQueries, LocaleQueries>();
        services.AddScoped<CMS.Application.Content.IEntryQueries, EntryQueries>();
        services.AddScoped<OutboxStore>();
        services.AddScoped<OutboxProcessor>();
        services.TryAddScoped<IOutboxSink, PlatformEventSink>();
        services.AddHostedService<OutboxWorker>();
        services.AddHealthChecks().AddCheck<SqlReadinessCheck>("sql", tags: ["ready"]);
        return services;
    }

    private static bool IsValidConnectionString(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            var builder = new SqlConnectionStringBuilder(value);
            return !string.IsNullOrWhiteSpace(builder.DataSource) &&
                !string.IsNullOrWhiteSpace(builder.InitialCatalog) &&
                !new[] { "master", "model", "msdb", "tempdb" }.Contains(builder.InitialCatalog, StringComparer.OrdinalIgnoreCase)
                && !builder.MultipleActiveResultSets;
        }
        catch (ArgumentException) { return false; }
    }
}
