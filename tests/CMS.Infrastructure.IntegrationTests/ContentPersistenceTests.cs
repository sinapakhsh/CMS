using CMS.Application.Content;
using CMS.Application.Platform.Locales;
using CMS.Application.Platform.Tenants;
using CMS.Domain.Content.Entries;
using CMS.Infrastructure.Persistence.Write;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Infrastructure.IntegrationTests;

// Verify schema constraints, Dapper projections, revisions and concurrency against real SQL Server.
public sealed class ContentPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    // Public readers cannot observe drafts or content scheduled for a later instant.
    [Fact]
    public async Task Content_lifecycle_is_isolated_versioned_and_conditionally_visible()
    {
        var tenantId = Guid.NewGuid();
        var slug = "content-" + tenantId.ToString("N");
        await using var scope = database.Scope(tenantId);
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        Assert.True((await sender.Send(new CreateTenantCommand(slug, "Test", "tenant"))).IsSuccess);
        var locale = await sender.Send(new CreateLocaleCommand("fa-IR", "فارسی", "locale"));
        var type = await sender.Send(new CreateContentTypeCommand("article", "Article", "{}", "type"));
        Assert.True(type.IsSuccess);
        var created = await sender.Send(new SaveEntryCommand(null, type.Value, locale.Value, "first", "First", "{}", null, "entry"));
        Assert.True(created.IsSuccess);
        var queries = scope.ServiceProvider.GetRequiredService<IEntryQueries>();
        Assert.Null(await queries.GetPublishedAsync(slug, "fa-IR", "first", default));
        var initial = Assert.Single(await queries.ListAsync(0, 10, default));
        Assert.True((await sender.Send(new TransitionEntryCommand(created.Value, "Review", null, Convert.ToBase64String(initial.RowVersion), "review"))).IsSuccess);
        var reviewed = Assert.Single(await queries.ListAsync(0, 10, default));
        Assert.True((await sender.Send(new TransitionEntryCommand(created.Value, "Published", database.Clock.UtcNow.AddHours(1), Convert.ToBase64String(reviewed.RowVersion), "publish"))).IsSuccess);
        Assert.Null(await queries.GetPublishedAsync(slug, "fa-IR", "first", default));
        database.Clock.UtcNow = database.Clock.UtcNow.AddHours(2);
        Assert.NotNull(await queries.GetPublishedAsync(slug, "fa-IR", "first", default));
        Assert.Null(await queries.GetPublishedAsync("another-tenant", "fa-IR", "first", default));
        Assert.Equal("Persistence.Concurrency", (await sender.Send(new SaveEntryCommand(created.Value, type.Value, locale.Value,
            "second", "Second", "{}", Convert.ToBase64String(initial.RowVersion), "stale"))).Error.Code);
        var current = Assert.Single(await queries.ListAsync(0, 10, default));
        Assert.True((await sender.Send(new SaveEntryCommand(created.Value, type.Value, locale.Value, "second", "Second", "{}",
            Convert.ToBase64String(current.RowVersion), "edit"))).IsSuccess);
        var context = scope.ServiceProvider.GetRequiredService<CmsDbContext>();
        context.ChangeTracker.Clear();
        var entry = await context.Set<Entry>().SingleAsync();
        Assert.Equal(2, entry.Revisions.Count);
        Assert.Equal("first", Assert.Single(entry.Redirects).Slug);
        Assert.Null(await queries.GetPublishedAsync(slug, "fa-IR", "first", default));
        await using var other = database.Scope(Guid.NewGuid());
        Assert.Empty(await other.ServiceProvider.GetRequiredService<IEntryQueries>().ListAsync(0, 10, default));
        Assert.True((await sender.Send(new RestoreEntryCommand(entry.Id, 1, Convert.ToBase64String(entry.RowVersion), "restore"))).IsSuccess);
        context.ChangeTracker.Clear();
        var restored = await context.Set<Entry>().SingleAsync();
        Assert.Equal("first", restored.Slug);
        Assert.Equal(3, restored.Revisions.Count);
    }
}
