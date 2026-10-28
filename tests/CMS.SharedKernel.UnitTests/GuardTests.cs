using CMS.SharedKernel.Guards;
using CMS.SharedKernel.Primitives;

namespace CMS.SharedKernel.UnitTests;

// Make invalid primitive inputs fail before reaching aggregate invariants.
public sealed class GuardTests
{
    // Guards validate text without silently normalizing meaningful whitespace.
    [Theory]
    [InlineData("value")]
    [InlineData(" value ")]
    public void Valid_text_is_preserved(string text) => Assert.Equal(text, Guard.NotBlank(text));

    // Caller argument names make invalid input failures diagnosable.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\t ")]
    public void Missing_text_identifies_the_argument(string? text)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => Guard.NotBlank(text));
        Assert.Equal("text", error.ParamName);
    }

    // GUID guards preserve real identifiers and reject uninitialized ones.
    [Fact]
    public void Identifier_guard_rejects_empty_values()
    {
        var id = Guid.NewGuid();
        Assert.Equal(id, Guard.NotEmpty(id));
        Assert.Throws<ArgumentException>(() => Guard.NotEmpty(Guid.Empty));
    }

    // A nonzero offset is rejected even when it represents the same instant.
    [Fact]
    public void Time_guard_requires_explicit_utc()
    {
        var now = new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);
        Assert.Equal(now, Guard.Utc(now));
        Assert.Throws<ArgumentException>(() => Guard.Utc(now.ToOffset(TimeSpan.FromHours(3.5))));
    }

    // Entity identity validation also handles reference-type identifiers.
    [Fact]
    public void Entity_rejects_null_identity()
    {
        Assert.Throws<ArgumentNullException>(() => new StringEntity(null!));
        Assert.Equal("entry", new StringEntity("entry").Id);
    }

    private sealed class StringEntity(string id) : Entity<string>(id);
}
