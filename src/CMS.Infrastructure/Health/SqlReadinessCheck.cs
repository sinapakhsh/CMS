using CMS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Health;

// Readiness checks the schema, not merely whether a SQL port accepts connections.
public sealed class SqlReadinessCheck(IOptions<DatabaseOptions> options) : IHealthCheck
{
    // Return a generic public status without disclosing connection strings or SQL exceptions.
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(options.Value.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 5;
            command.CommandText = """
                SELECT CASE WHEN OBJECT_ID(N'plt_tenants', N'U') IS NOT NULL
                     AND OBJECT_ID(N'plt_locales', N'U') IS NOT NULL
                     AND OBJECT_ID(N'plt_audit', N'U') IS NOT NULL
                     AND OBJECT_ID(N'plt_outbox', N'U') IS NOT NULL
                     AND OBJECT_ID(N'plt_idempotency', N'U') IS NOT NULL THEN 1 ELSE 0 END;
                """;
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database schema is not ready.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (SqlException) { return HealthCheckResult.Unhealthy("Database is unavailable."); }
    }
}
