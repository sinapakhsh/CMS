using System.Data;
using CMS.Application.Common.Abstractions;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.Persistence.Write;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Messaging;

// This operational store intentionally spans tenants and is never exposed to request handlers.
public sealed class OutboxStore(IOptions<DatabaseOptions> database, IOptions<OutboxOptions> options, IClock clock)
{
    // Atomic update-and-output ensures two workers cannot claim the same live lease.
    public async Task<OutboxMessage?> ClaimAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(database.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SET NOCOUNT ON;
            UPDATE plt_outbox SET dead_letter_at_utc=@now, last_error=COALESCE(last_error, 'LeaseExpired')
            WHERE processed_at_utc IS NULL AND dead_letter_at_utc IS NULL AND attempts >= @maxAttempts
              AND (locked_until_utc IS NULL OR locked_until_utc <= @now);
            ;WITH candidate AS (
                SELECT TOP (1) * FROM plt_outbox WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE processed_at_utc IS NULL AND dead_letter_at_utc IS NULL
                  AND attempts < @maxAttempts AND next_attempt_at_utc <= @now
                  AND (locked_until_utc IS NULL OR locked_until_utc <= @now)
                ORDER BY next_attempt_at_utc, id
            )
            UPDATE candidate SET lock_token=@token, locked_until_utc=@until, attempts=attempts+1
            OUTPUT inserted.id, inserted.event_id, inserted.tenant_id, inserted.event_type,
                   inserted.payload, inserted.attempts, inserted.lock_token, inserted.locked_until_utc;
            """;
        command.Parameters.Add("@now", SqlDbType.DateTimeOffset).Value = clock.UtcNow;
        command.Parameters.Add("@until", SqlDbType.DateTimeOffset).Value = clock.UtcNow.AddSeconds(options.Value.LeaseSeconds);
        command.Parameters.Add("@token", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
        command.Parameters.Add("@maxAttempts", SqlDbType.Int).Value = options.Value.MaxAttempts;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new OutboxMessage
        {
            Id = reader.GetGuid(0),
            EventId = reader.GetGuid(1),
            TenantId = reader.GetGuid(2),
            EventType = reader.GetString(3),
            Payload = reader.GetString(4),
            Attempts = reader.GetInt32(5),
            LockToken = reader.GetGuid(6),
            LockedUntilUtc = reader.GetDateTimeOffset(7)
        };
    }

    // A worker that lost its lease cannot acknowledge a newer owner's work.
    public Task<bool> CompleteAsync(OutboxMessage message, CancellationToken cancellationToken) =>
        UpdateAsync(message, null, cancellationToken);

    // Failed attempts release the lease and schedule bounded exponential backoff.
    public Task<bool> FailAsync(OutboxMessage message, string errorCategory, CancellationToken cancellationToken) =>
        UpdateAsync(message, errorCategory, cancellationToken);

    private async Task<bool> UpdateAsync(OutboxMessage message, string? error, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(database.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = error is null
            ? """
              UPDATE plt_outbox SET processed_at_utc=@now, lock_token=NULL, locked_until_utc=NULL, last_error=NULL
              WHERE id=@id AND lock_token=@token AND locked_until_utc > @now AND processed_at_utc IS NULL;
              """
            : """
              UPDATE plt_outbox SET lock_token=NULL, locked_until_utc=NULL, last_error=@error,
                  next_attempt_at_utc=@next,
                  dead_letter_at_utc=CASE WHEN attempts >= @maxAttempts THEN @now ELSE NULL END
              WHERE id=@id AND lock_token=@token AND locked_until_utc > @now AND processed_at_utc IS NULL;
              """;
        command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = message.Id;
        command.Parameters.Add("@token", SqlDbType.UniqueIdentifier).Value = (object?)message.LockToken ?? DBNull.Value;
        command.Parameters.Add("@now", SqlDbType.DateTimeOffset).Value = clock.UtcNow;
        if (error is not null)
        {
            command.Parameters.Add("@error", SqlDbType.NVarChar, 200).Value = error[..Math.Min(error.Length, 200)];
            command.Parameters.Add("@next", SqlDbType.DateTimeOffset).Value =
                clock.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, Math.Min(message.Attempts, 8))));
            command.Parameters.Add("@maxAttempts", SqlDbType.Int).Value = options.Value.MaxAttempts;
        }
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }
}
