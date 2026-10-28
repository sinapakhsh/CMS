using CMS.Application;
using CMS.Application.Common.Abstractions;
using CMS.Application.Platform.Locales;
using CMS.Application.Platform.Tenants;
using CMS.Infrastructure;
using CMS.Infrastructure.Persistence;
using CMS.Migrator;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

if (args.Length == 0 || (args[0] == "migrate" ? args.Length != 1 :
    args[0] == "create-admin" ? args.Length != 3 :
    args[0] is not ("provision" or "create-locale") || args.Length != 5))
{
    Console.Error.WriteLine("Usage: migrate | provision <tenant-id> <slug> <name> <key> | create-locale <tenant-id> <code> <name> <key> | create-admin <tenant-id> <email> (password: CMS_ADMIN_PASSWORD environment variable)");
    return 2;
}
Guid? tenantId = null;
if (args[0] != "migrate")
{
    if (!Guid.TryParse(args[1], out var parsed) || parsed == Guid.Empty)
    {
        Console.Error.WriteLine("A nonempty tenant identifier is required.");
        return 2;
    }
    tenantId = parsed;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
try
{
    var builder = Host.CreateApplicationBuilder();
    var identity = new AdministrativeIdentity(tenantId);
    builder.Services.AddSingleton<ICurrentTenant>(identity);
    builder.Services.AddSingleton<ICurrentUser>(identity);
    builder.Services.AddCmsApplication();
    builder.Services.AddCmsInfrastructure(builder.Configuration);
    using var host = builder.Build();
    await using var scope = host.Services.CreateAsyncScope();
    _ = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    if (args[0] == "migrate")
    {
        await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync(cancellation.Token);
        Console.WriteLine("Database migrations applied successfully.");
        return 0;
    }
    if (args[0] == "create-admin")
    {
        var provisioned = await scope.ServiceProvider.GetRequiredService<CMS.Infrastructure.Identity.AdministratorProvisioner>()
            .CreateAsync(args[2], Environment.GetEnvironmentVariable("CMS_ADMIN_PASSWORD") ?? string.Empty, cancellation.Token);
        Console.WriteLine(provisioned.IsSuccess ? provisioned.Value.ToString() : provisioned.Error.Code);
        return provisioned.IsSuccess ? 0 : 1;
    }
    ICommand command = args[0] == "provision"
        ? new CreateTenantCommand(args[2], args[3], args[4])
        : new CreateLocaleCommand(args[2], args[3], args[4]);
    var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(command, cancellation.Token);
    if (result.IsFailure)
    {
        Console.Error.WriteLine($"{result.Error.Code}: {result.Error.Description}");
        return 1;
    }
    Console.WriteLine(result.Value);
    return 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Operation cancelled.");
    return 1;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Operation failed ({exception.GetType().Name}). Verify configuration and database access.");
    return 1;
}
