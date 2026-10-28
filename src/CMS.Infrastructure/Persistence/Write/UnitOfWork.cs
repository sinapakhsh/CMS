using System.Data;
using System.Security.Cryptography;
using System.Text;
using CMS.Application.Common.Abstractions;
using CMS.Domain.Platform;
using CMS.Infrastructure.Messaging;
using CMS.SharedKernel.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CMS.Infrastructure.Persistence.Write;

// A single transaction owns aggregate changes, audit, outbox and successful replay metadata.
public sealed class UnitOfWork(CmsDbContext context, ICurrentTenant tenant, ICurrentUser user, IClock clock, IIdGenerator ids) : IUnitOfWork
{
    // A nested pipeline joins the outer transaction and cannot commit it prematurely.
    public async Task<Result<Guid>> ExecuteAsync(Func<Task<Result<Guid>>> action, CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null || tenant.TenantId == Guid.Empty)
            return Result<Guid>.Failure(new Error("Tenant.Required", "A trusted tenant scope is required."));
        if (context.Database.CurrentTransaction is not null) return await action();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action();
            if (result.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                context.ChangeTracker.Clear();
                return result;
            }

            var aggregates = PrepareChanges();
            await context.PersistAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            context.ChangeTracker.AcceptAllChanges();
            foreach (var aggregate in aggregates) aggregate.ClearDomainEvents();
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return Result<Guid>.Failure(new Error("Persistence.Concurrency", "The record changed. Reload it before retrying."));
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return Result<Guid>.Failure(new Error("Persistence.Conflict", "A unique record already exists."));
        }
        catch
        {
            context.ChangeTracker.Clear();
            throw;
        }
    }

    // Transaction-owned application locks serialize only competing requests for the same key.
    public Task<Result<Guid>> ExecuteIdempotentAsync(string operation, string key, string fingerprint,
        Func<Task<Result<Guid>>> action, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (operation.Length > 200 || key.Length > 128 || fingerprint.Length != 64)
            throw new ArgumentException("Invalid idempotency metadata.");

        return ExecuteAsync(async () =>
        {
            var resource = "cms:idempotency:" + Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes($"{tenant.TenantId:N}|{operation}|{key}")));
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.Transaction = context.Database.CurrentTransaction!.GetDbTransaction();
            command.CommandText = """
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=10000;
                SELECT @result;
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@resource";
            parameter.Value = resource;
            command.Parameters.Add(parameter);
            var lockResult = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            if (lockResult < 0)
                return Result<Guid>.Failure(new Error("Idempotency.Busy", "This request is already being processed. Retry later."));

            var record = await context.IdempotencyRecords.SingleOrDefaultAsync(
                row => row.Operation == operation && row.Key == key, cancellationToken);
            if (record is not null && record.ExpiresAtUtc > clock.UtcNow)
                return record.Fingerprint == fingerprint
                    ? Result<Guid>.Success(record.ResultId)
                    : Result<Guid>.Failure(new Error("Idempotency.Conflict", "This key was used with a different payload."));

            var result = await action();
            if (result.IsSuccess)
            {
                if (record is null)
                {
                    record = new IdempotencyRecord { Id = ids.NewId(), TenantId = tenant.TenantId!.Value, Operation = operation, Key = key };
                    context.IdempotencyRecords.Add(record);
                }
                record.Fingerprint = fingerprint;
                record.ResultId = result.Value;
                record.ExpiresAtUtc = clock.UtcNow.AddHours(24);
            }
            return result;
        }, cancellationToken);
    }

    private TenantAggregate[] PrepareChanges()
    {
        context.ChangeTracker.DetectChanges();
        var entries = context.ChangeTracker.Entries<TenantAggregate>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        foreach (var entry in entries)
        {
            if (entry.Entity.TenantId != tenant.TenantId ||
                (entry.State != EntityState.Added && entry.Property(entity => entity.TenantId).OriginalValue != tenant.TenantId))
                throw new InvalidOperationException("Cross-tenant writes are prohibited.");

            context.AuditRecords.Add(new AuditRecord
            {
                Id = ids.NewId(),
                TenantId = entry.Entity.TenantId,
                ActorId = user.UserId,
                EntityId = entry.Entity.Id,
                EntityType = entry.Metadata.ClrType.Name,
                Operation = entry.State.ToString(),
                OccurredAtUtc = clock.UtcNow
            });
            foreach (var domainEvent in entry.Entity.DomainEvents)
                context.OutboxMessages.Add(new OutboxMessage
                {
                    Id = ids.NewId(),
                    EventId = domainEvent.EventId,
                    TenantId = entry.Entity.TenantId,
                    EventType = EventSerializer.TypeName(domainEvent),
                    Payload = EventSerializer.Serialize(domainEvent),
                    OccurredAtUtc = domainEvent.OccurredAtUtc,
                    NextAttemptAtUtc = clock.UtcNow
                });
        }
        return entries.Select(entry => entry.Entity).ToArray();
    }
}
