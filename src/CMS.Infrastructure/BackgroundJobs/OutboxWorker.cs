using CMS.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.BackgroundJobs;

// A failed database poll must not terminate the host or create a tight retry loop.
public sealed class OutboxWorker(IServiceScopeFactory scopes, IOptions<OutboxOptions> options,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    // Each delivery uses a fresh scope to avoid carrying tenant or tracking state across events.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                if (await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcessOnceAsync(stoppingToken))
                    continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogWarning("Outbox polling failed: {ErrorCategory}", exception.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(options.Value.PollSeconds), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
