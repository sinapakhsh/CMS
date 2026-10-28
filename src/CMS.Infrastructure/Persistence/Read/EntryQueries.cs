using CMS.Application.Common.Abstractions;
using CMS.Application.Content;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Persistence.Read;

// Parameterized projections include tenant scope in the SQL itself.
public sealed class EntryQueries(IOptions<DatabaseOptions> database, ICurrentTenant tenant, IClock clock) : IEntryQueries
{
    // Redirect aliases resolve to the current entry without exposing draft or future content.
    public async Task<EntryDto?> GetPublishedAsync(string tenantSlug, string locale, string slug, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(database.Value.ConnectionString);
        return await connection.QueryFirstOrDefaultAsync<EntryDto>(new CommandDefinition("""
            SELECT TOP (1) e.id AS Id, e.content_type_id AS ContentTypeId, e.locale_id AS LocaleId,
                e.slug AS Slug, e.title AS Title, e.fields_json AS FieldsJson, e.status AS Status,
                e.publish_at_utc AS PublishAtUtc, e.row_version AS RowVersion
            FROM cnt_entries e
            JOIN plt_tenants t ON t.id=e.tenant_id
            JOIN plt_locales l ON l.id=e.locale_id AND l.tenant_id=e.tenant_id
            WHERE t.slug=@Tenant AND l.code=@Locale AND e.status='Published'
                AND (e.publish_at_utc IS NULL OR e.publish_at_utc <= @Now)
                AND (e.slug=@Slug OR EXISTS (SELECT 1 FROM cnt_redirects r
                    WHERE r.tenant_id=e.tenant_id AND r.entry_id=e.id AND r.locale_id=e.locale_id AND r.slug=@Slug))
            ORDER BY CASE WHEN e.slug=@Slug THEN 0 ELSE 1 END, e.id;
            """, new { Tenant = tenantSlug, Locale = locale, Slug = slug, Now = clock.UtcNow }, cancellationToken: cancellationToken));
    }

    // Ordering includes a unique key so page boundaries are deterministic.
    public async Task<IReadOnlyList<EntryDto>> ListAsync(int offset, int limit, CancellationToken cancellationToken)
    {
        if (tenant.TenantId is not { } tenantId || tenantId == Guid.Empty) throw new InvalidOperationException("A trusted tenant scope is required.");
        await using var connection = new SqlConnection(database.Value.ConnectionString);
        var rows = await connection.QueryAsync<EntryDto>(new CommandDefinition("""
            SELECT id AS Id, content_type_id AS ContentTypeId, locale_id AS LocaleId, slug AS Slug, title AS Title,
                fields_json AS FieldsJson, status AS Status, publish_at_utc AS PublishAtUtc, row_version AS RowVersion
            FROM cnt_entries WHERE tenant_id=@TenantId ORDER BY id OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
            """, new { TenantId = tenantId, Offset = Math.Clamp(offset, 0, 100000), Limit = Math.Clamp(limit, 1, 100) }, cancellationToken: cancellationToken));
        return rows.AsList();
    }
}
