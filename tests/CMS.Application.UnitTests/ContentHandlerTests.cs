using CMS.Application.Common.Abstractions;
using CMS.Application.Content;
using CMS.Domain.Content.ContentTypes;
using CMS.Domain.Content.Entries;
using CMS.Domain.Platform.Locales;
using NSubstitute;

namespace CMS.Application.UnitTests;

// Verify orchestration rejects missing relationships and stale edits before changing domain state.
public sealed class ContentHandlerTests
{
    // Invalid schemas never reach persistence; a valid definition stages exactly one aggregate.
    [Fact]
    public async Task Content_type_creation_validates_before_tracking()
    {
        var repository = Substitute.For<IRepository<ContentType>>();
        var tenant = Substitute.For<ICurrentTenant>();
        var ids = Substitute.For<IIdGenerator>();
        tenant.TenantId.Returns(Guid.NewGuid());
        ids.NewId().Returns(Guid.NewGuid());
        var handler = new CreateContentTypeHandler(repository, tenant, ids);
        Assert.True((await handler.Handle(new("article", "Article", "{}", "key"), default)).IsSuccess);
        Assert.True((await handler.Handle(new("article", "Article", "not-json", "key"), default)).IsFailure);
        repository.Received(1).Add(Arg.Any<ContentType>());
    }

    // Editing cannot silently change an entry's tenant, type, locale or last-observed version.
    [Fact]
    public async Task Entry_save_checks_relationships_and_concurrency()
    {
        var tenantId = Guid.NewGuid();
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.TenantId.Returns(tenantId);
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        var ids = Substitute.For<IIdGenerator>();
        ids.NewId().Returns(Guid.NewGuid());
        var entries = Substitute.For<IRepository<Entry>>();
        var types = Substitute.For<IRepository<ContentType>>();
        var locales = Substitute.For<IRepository<Locale>>();
        var type = ContentType.Create(Guid.NewGuid(), tenantId, "article", "Article", "{}").Value;
        var locale = Locale.Create(Guid.NewGuid(), tenantId, "fa-IR", "فارسی", clock.UtcNow).Value;
        var request = new SaveEntryCommand(null, type.Id, locale.Id, "first", "First", "{}", null, "key");
        var handler = new SaveEntryHandler(entries, types, locales, tenant, ids, clock);
        Assert.True((await handler.Handle(request, default)).IsFailure);
        types.GetByIdAsync(type.Id, default).Returns(type);
        locales.GetByIdAsync(locale.Id, default).Returns(locale);
        Assert.True((await handler.Handle(request, default)).IsSuccess);
        var entry = Entry.Create(Guid.NewGuid(), tenantId, type, locale.Id, "first", "First", "{}", clock.UtcNow).Value;
        entries.GetByIdAsync(entry.Id, default).Returns(entry);
        Assert.Equal("Persistence.Concurrency", (await handler.Handle(request with { Id = entry.Id, RowVersion = "stale" }, default)).Error.Code);
        Assert.True((await handler.Handle(request with { Id = entry.Id, RowVersion = "", Title = "Edited" }, default)).IsSuccess);
        Assert.Equal("Edited", entry.Title);
    }

    // Workflow and restore expose domain failures without partially changing the entity.
    [Fact]
    public async Task Workflow_and_restore_preserve_success_and_rejection()
    {
        var tenantId = Guid.NewGuid();
        var type = ContentType.Create(Guid.NewGuid(), tenantId, "article", "Article", "{}").Value;
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        var entry = Entry.Create(Guid.NewGuid(), tenantId, type, Guid.NewGuid(), "first", "First", "{}", clock.UtcNow).Value;
        var entries = Substitute.For<IRepository<Entry>>();
        var types = Substitute.For<IRepository<ContentType>>();
        entries.GetByIdAsync(entry.Id, default).Returns(entry);
        types.GetByIdAsync(type.Id, default).Returns(type);
        var transition = new TransitionEntryHandler(entries);
        Assert.True((await transition.Handle(new(entry.Id, "Published", null, "", "key"), default)).IsFailure);
        Assert.True((await transition.Handle(new(entry.Id, "Review", null, "", "key"), default)).IsSuccess);
        var restore = new RestoreEntryHandler(entries, types, clock);
        Assert.True((await restore.Handle(new(entry.Id, 99, "", "key"), default)).IsFailure);
        Assert.True((await restore.Handle(new(entry.Id, 1, "", "key"), default)).IsSuccess);
        Assert.Equal("Draft", entry.Status);
    }

    // Management pagination is bounded and public route rejection avoids SQL entirely.
    [Fact]
    public async Task Read_handlers_bound_inputs_and_propagate_failures()
    {
        var queries = Substitute.For<IEntryQueries>();
        IReadOnlyList<EntryDto> rows = [];
        queries.ListAsync(0, 100, default).Returns(rows);
        var list = new ListEntriesHandler(queries);
        Assert.Same(rows, await list.Handle(new(-1, 500), default));
        queries.ListAsync(0, 100, default).Returns<Task<IReadOnlyList<EntryDto>>>(_ => throw new InvalidOperationException());
        await Assert.ThrowsAsync<InvalidOperationException>(() => list.Handle(new(-1, 500), default));
        var publication = new PublicEntryHandler(queries);
        Assert.Null(await publication.Handle(new(new string('x', 64), "fa", "slug"), default));
        var dto = new EntryDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "slug", "Title", "{}", "Published", null, []);
        queries.GetPublishedAsync("tenant", "fa", "slug", default).Returns(dto);
        Assert.Same(dto, await publication.Handle(new("tenant", "fa", "slug"), default));
    }
}
