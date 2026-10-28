using CMS.Application.Common.Abstractions;
using CMS.Application.Common.Models;
using CMS.Application.Platform.Locales;
using CMS.Application.Platform.Tenants;
using CMS.Domain.Platform.Locales;
using CMS.Domain.Platform.Tenants;
using NSubstitute;

namespace CMS.Application.UnitTests;

// Handlers must distinguish expected rejection from successful staging without saving directly.
public sealed class PlatformHandlerTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);

    // Successful provisioning stages the root with the trusted identity.
    [Fact]
    public async Task Create_tenant_stages_a_valid_aggregate()
    {
        var repository = Substitute.For<IRepository<Tenant>>();
        var tenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IClock>();
        var id = Guid.NewGuid();
        tenant.TenantId.Returns(id);
        clock.UtcNow.Returns(_now);
        var result = await new CreateTenantHandler(repository, tenant, clock)
            .Handle(new CreateTenantCommand("site", "Site", "key"), CancellationToken.None);
        Assert.Equal(id, result.Value);
        repository.Received(1).Add(Arg.Is<Tenant>(value => value.Id == id));
    }

    // Existing roots and untrusted scopes cannot create another tenant.
    [Fact]
    public async Task Create_tenant_rejects_existing_or_missing_scope()
    {
        var repository = Substitute.For<IRepository<Tenant>>();
        var tenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IClock>();
        var handler = new CreateTenantHandler(repository, tenant, clock);
        Assert.Equal("Tenant.Required", (await handler.Handle(new("site", "Site", "key"), default)).Error.Code);
        var id = Guid.NewGuid();
        tenant.TenantId.Returns(id);
        repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(Tenant.Create(id, "site", "Site", _now).Value);
        Assert.Equal("Tenant.Exists", (await handler.Handle(new("site", "Site", "key"), default)).Error.Code);
        repository.DidNotReceive().Add(Arg.Any<Tenant>());
    }

    // Locale handlers stage only data belonging to the resolved tenant.
    [Fact]
    public async Task Create_locale_stages_and_rejects_duplicates()
    {
        var locales = Substitute.For<IRepository<Locale>>();
        var tenants = Substitute.For<IRepository<Tenant>>();
        var tenant = Substitute.For<ICurrentTenant>();
        var clock = Substitute.For<IClock>();
        var ids = Substitute.For<IIdGenerator>();
        var tenantId = Guid.NewGuid();
        var localeId = Guid.NewGuid();
        tenant.TenantId.Returns(tenantId);
        clock.UtcNow.Returns(_now);
        ids.NewId().Returns(localeId);
        tenants.GetByIdAsync(tenantId, Arg.Any<CancellationToken>()).Returns(Tenant.Create(tenantId, "site", "Site", _now).Value);
        var handler = new CreateLocaleHandler(locales, tenants, tenant, ids, clock);
        var result = await handler.Handle(new("fa-IR", "Persian", "key"), default);
        Assert.Equal(localeId, result.Value);
        locales.Received(1).Add(Arg.Is<Locale>(locale => locale.TenantId == tenantId));
        locales.AnyAsync(Arg.Any<Specification<Locale>>(), Arg.Any<CancellationToken>()).Returns(true);
        Assert.Equal("Locale.Exists", (await handler.Handle(new("fa-IR", "Persian", "new-key"), default)).Error.Code);
        Assert.Equal("Locale.Invalid", (await handler.Handle(new("invalid-culture", "Name", "key"), default)).Error.Code);
    }

    // Missing tenant roots cannot create orphan language records.
    [Fact]
    public async Task Create_locale_rejects_missing_tenant()
    {
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.TenantId.Returns(Guid.NewGuid());
        var ids = Substitute.For<IIdGenerator>();
        ids.NewId().Returns(Guid.NewGuid());
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(_now);
        var locales = Substitute.For<IRepository<Locale>>();
        var handler = new CreateLocaleHandler(locales, Substitute.For<IRepository<Tenant>>(), tenant, ids, clock);
        Assert.Equal("Tenant.NotFound", (await handler.Handle(new("fa", "Persian", "key"), default)).Error.Code);
        locales.DidNotReceive().Add(Arg.Any<Locale>());
    }

    // Read handlers preserve cancellation and return the read model without tracking.
    [Fact]
    public async Task List_locales_returns_query_results_and_propagates_failures()
    {
        var queries = Substitute.For<ILocaleQueries>();
        IReadOnlyList<LocaleDto> rows = [new(Guid.NewGuid(), "fa-IR", "Persian", true)];
        queries.ListAsync(Arg.Any<CancellationToken>()).Returns(rows);
        var handler = new ListLocalesHandler(queries);
        Assert.Same(rows, await handler.Handle(new ListLocalesQuery(), default));
        queries.ListAsync(Arg.Any<CancellationToken>()).Returns<Task<IReadOnlyList<LocaleDto>>>(_ => throw new InvalidOperationException("Missing tenant."));
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new ListLocalesQuery(), default));
    }
}
