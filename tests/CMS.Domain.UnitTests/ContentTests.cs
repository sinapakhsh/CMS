using CMS.Domain.Content.ContentTypes;
using CMS.Domain.Content.Entries;

namespace CMS.Domain.UnitTests;

// Content validation and publication rules cannot depend on the transport layer.
public sealed class ContentTests
{
    // Required, unknown, duplicate and incorrectly typed fields are all rejected.
    [Theory]
    [InlineData("{\"body\":\"hello\"}", true)]
    [InlineData("{}", false)]
    [InlineData("{\"body\":null}", false)]
    [InlineData("{\"body\":12}", false)]
    [InlineData("{\"body\":\"x\",\"extra\":true}", false)]
    [InlineData("{\"body\":\"x\",\"body\":\"y\"}", false)]
    [InlineData("invalid", false)]
    public void Fields_follow_the_declared_schema(string json, bool expected)
    {
        var type = ContentType.Create(Guid.NewGuid(), Guid.NewGuid(), "article", "Article", "{\"body\":{\"type\":\"text\",\"required\":true}}").Value;
        Assert.Equal(expected, type.Accepts(json));
    }

    // Publication requires review and restoring a revision creates a new draft snapshot.
    [Fact]
    public void Publication_and_revision_restore_preserve_history()
    {
        var tenant = Guid.NewGuid();
        var type = ContentType.Create(Guid.NewGuid(), tenant, "article", "Article", "{}").Value;
        var now = DateTimeOffset.UtcNow;
        var entry = Entry.Create(Guid.NewGuid(), tenant, type, Guid.NewGuid(), "اول", "First", "{}", now).Value;
        Assert.True(entry.Transition("Published", null).IsFailure);
        Assert.True(entry.Transition("Review", null).IsSuccess);
        Assert.True(entry.Transition("Published", now.AddHours(1)).IsSuccess);
        Assert.True(entry.Edit(type, "دوم", "Second", "{}", now).IsSuccess);
        Assert.Equal("Draft", entry.Status);
        Assert.Equal("اول", Assert.Single(entry.Redirects).Slug);
        Assert.True(entry.Restore(type, 1, now).IsSuccess);
        Assert.Equal("اول", entry.Slug);
        Assert.Equal(3, entry.Revisions.Count);
        Assert.Contains(entry.Redirects, redirect => redirect.Slug == "دوم");
        Assert.True(entry.Restore(type, 100, now).IsFailure);
    }
}
