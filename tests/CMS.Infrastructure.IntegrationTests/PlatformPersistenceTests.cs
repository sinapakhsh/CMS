using CMS.Application.Common.Abstractions;
using CMS.Application.Platform.Locales;
using CMS.Application.Platform.Tenants;
using CMS.Domain.Platform.Locales;
using CMS.Infrastructure.Messaging;
using CMS.Infrastructure.Persistence.Write;
using CMS.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Infrastructure.IntegrationTests;

// Exercise production migrations and SQL semantics rather than an in-memory approximation.
public sealed class PlatformPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    // Competing retries serialize on the SQL application lock and return the same committed identity.
    [Fact]
    public async Task Concurrent_retries_create_exactly_one_locale()
    {
        var tenantId = Guid.NewGuid();
        await using (var scope = database.Scope(tenantId))
            Assert.True((await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new CreateTenantCommand("concurrent-" + tenantId.ToString("N"), "Concurrent", "tenant"))).IsSuccess);
        async Task<Guid> CreateAsync()
        {
            await using var scope = database.Scope(tenantId);
            var result = await scope.ServiceProvider.GetRequiredService<ISender>()
                .Send(new CreateLocaleCommand("en-US", "English", "same-key"));
            Assert.True(result.IsSuccess);
            return result.Value;
        }
        var results = await Task.WhenAll(CreateAsync(), CreateAsync());
        Assert.Equal(results[0], results[1]);
        await using var check = database.Scope(tenantId);
        Assert.Single(await check.ServiceProvider.GetRequiredService<ILocaleQueries>().ListAsync(default));
    }

    // Replay must not duplicate business rows, audit records or durable events.
    [Fact]
    public async Task Commands_are_atomic_idempotent_and_tenant_isolated()
    {
        var tenantId = Guid.NewGuid();
        Guid localeId;
        await using (var scope = database.Scope(tenantId))
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            Assert.True((await sender.Send(new CreateTenantCommand("tenant-" + tenantId.ToString("N"), "Test", "tenant"))).IsSuccess);
            var command = new CreateLocaleCommand("fa-IR", "فارسی", "locale");
            var created = await sender.Send(command);
            Assert.True(created.IsSuccess);
            localeId = created.Value;
            Assert.Equal(localeId, (await sender.Send(command)).Value);
            Assert.Equal("Idempotency.Conflict", (await sender.Send(command with { DisplayName = "Different" })).Error.Code);
            var context = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
            Assert.Equal(2, await context.AuditRecords.CountAsync());
            Assert.Equal(2, await context.OutboxMessages.CountAsync());
            Assert.Equal(2, await context.IdempotencyRecords.CountAsync());
            var locales = await sender.Send(new ListLocalesQuery());
            Assert.Equal(localeId, Assert.Single(locales).Id);
            Assert.True(locales[0].IsRightToLeft);
        }
        await using (var other = database.Scope(Guid.NewGuid()))
        {
            Assert.Empty(await other.ServiceProvider.GetRequiredService<ISender>().Send(new ListLocalesQuery()));
            Assert.Null(await other.ServiceProvider.GetRequiredService<IRepository<Locale>>().GetByIdAsync(localeId, default));
        }
        await using (var anonymous = database.Scope(null))
        {
            Assert.Empty(await anonymous.ServiceProvider.GetRequiredService<CmsDbContext>().Locales.ToListAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(() => anonymous.ServiceProvider.GetRequiredService<ILocaleQueries>().ListAsync(default));
        }
    }

    // A failed operation cannot persist its pending changes or poison the following command.
    [Fact]
    public async Task Failed_transaction_discards_pending_rows()
    {
        var tenantId = Guid.NewGuid();
        await using var scope = database.Scope(tenantId);
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        Assert.True((await sender.Send(new CreateTenantCommand("rollback-" + tenantId.ToString("N"), "Test", "tenant"))).IsSuccess);
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Locale>>();
        var result = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ExecuteAsync(() =>
        {
            repository.Add(Locale.Create(Guid.NewGuid(), tenantId, "en-US", "English", database.Clock.UtcNow).Value);
            return Task.FromResult(Result<Guid>.Failure(new Error("Test.Failure", "Abort")));
        }, default);
        Assert.True(result.IsFailure);
        Assert.Empty(await sender.Send(new ListLocalesQuery()));
        Assert.True((await sender.Send(new CreateLocaleCommand("en-US", "English", "retry"))).IsSuccess);
    }

    // Lease ownership protects acknowledgement against another worker's takeover.
    [Fact]
    public async Task Outbox_claims_are_exclusive_and_can_be_acknowledged()
    {
        var tenantId = Guid.NewGuid();
        await using var scope = database.Scope(tenantId);
        Assert.True((await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new CreateTenantCommand("outbox-" + tenantId.ToString("N"), "Test", "tenant"))).IsSuccess);
        var store = scope.ServiceProvider.GetRequiredService<OutboxStore>();
        var message = await store.ClaimAsync(default);
        Assert.NotNull(message);
        var original = message.LockToken;
        message.LockToken = Guid.NewGuid();
        Assert.False(await store.CompleteAsync(message, default));
        message.LockToken = original;
        Assert.True(await store.CompleteAsync(message, default));
        Assert.False(await store.CompleteAsync(message, default));
    }
}
