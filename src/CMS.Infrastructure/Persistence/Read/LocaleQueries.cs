using CMS.Application.Common.Abstractions;
using CMS.Application.Platform.Locales;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Persistence.Read;

// Dapper is restricted to parameterized read projections.
public sealed class LocaleQueries(IOptions<DatabaseOptions> options, ICurrentTenant tenant) : ILocaleQueries
{
    // The tenant predicate is mandatory and the locale catalogue has a defensive bound.
    public async Task<IReadOnlyList<LocaleDto>> ListAsync(CancellationToken cancellationToken)
    {
        if (tenant.TenantId is not { } tenantId || tenantId == Guid.Empty)
            throw new InvalidOperationException("A trusted tenant scope is required.");
        await using var connection = new SqlConnection(options.Value.ConnectionString);
        var rows = await connection.QueryAsync<LocaleDto>(new CommandDefinition(
            """
            SELECT TOP (1000) id AS Id, code AS Code, display_name AS DisplayName, is_right_to_left AS IsRightToLeft
            FROM plt_locales WHERE tenant_id = @TenantId ORDER BY code;
            """, new { TenantId = tenantId }, cancellationToken: cancellationToken));
        return rows.AsList();
    }
}
