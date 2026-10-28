using CMS.Domain.Platform.Locales;
using CMS.Domain.Platform.Tenants;

namespace CMS.Domain.UnitTests;

// Test invariants independently of EF, HTTP and mediator validation.
public sealed class PlatformTests
{
    private static readonly DateTimeOffset _now = new(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);

    // Tenant identity must agree with the ownership identity persisted by SQL.
    [Fact]
    public void Tenant_normalizes_slug_and_raises_one_event()
    {
        var id = Guid.NewGuid();
        var result = Tenant.Create(id, " EXAMPLE-SITE ", " Example ", _now);
        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.TenantId);
        Assert.Equal("example-site", result.Value.Slug);
        Assert.Equal("Example", result.Value.Name);
        var domainEvent = Assert.IsType<TenantCreated>(Assert.Single(result.Value.DomainEvents));
        Assert.Equal(id, domainEvent.TenantId);
        Assert.Equal(_now, domainEvent.OccurredAtUtc);
    }

    // Invalid slugs must never enter the persistence model.
    [Theory]
    [InlineData("")]
    [InlineData("bad_slug")]
    [InlineData("-site")]
    [InlineData("site-")]
    [InlineData("a--b")]
    public void Tenant_rejects_invalid_slugs(string slug) =>
        Assert.True(Tenant.Create(Guid.NewGuid(), slug, "Name", _now).IsFailure);

    // Identity, name and UTC time are checked even when no validator is involved.
    [Fact]
    public void Tenant_rejects_invalid_identity_name_time_and_bounds()
    {
        Assert.True(Tenant.Create(Guid.Empty, "site", "Name", _now).IsFailure);
        Assert.True(Tenant.Create(Guid.NewGuid(), "site", " ", _now).IsFailure);
        Assert.True(Tenant.Create(Guid.NewGuid(), "site", "Name", _now.ToOffset(TimeSpan.FromHours(1))).IsFailure);
        Assert.True(Tenant.Create(Guid.NewGuid(), new string('a', 64), "Name", _now).IsFailure);
        Assert.True(Tenant.Create(Guid.NewGuid(), "site", new string('a', 201), _now).IsFailure);
        Assert.True(Tenant.Create(Guid.NewGuid(), new string('a', 63), new string('a', 200), _now).IsSuccess);
    }

    // Language direction follows the culture rather than caller-controlled flags.
    [Theory]
    [InlineData("fa-IR", true)]
    [InlineData("en-US", false)]
    public void Locale_preserves_tenant_and_derives_direction(string code, bool rtl)
    {
        var tenantId = Guid.NewGuid();
        var result = Locale.Create(Guid.NewGuid(), tenantId, code, " Label ", _now);
        Assert.True(result.IsSuccess);
        Assert.Equal(tenantId, result.Value.TenantId);
        Assert.Equal(code, result.Value.Code);
        Assert.Equal(rtl, result.Value.IsRightToLeft);
        Assert.Equal("Label", result.Value.DisplayName);
        Assert.IsType<LocaleCreated>(Assert.Single(result.Value.DomainEvents));
    }

    // Invalid and invented cultures cannot become another module's language source.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-real-culture")]
    [InlineData("fa_IR")]
    public void Locale_rejects_invalid_culture(string code) =>
        Assert.True(Locale.Create(Guid.NewGuid(), Guid.NewGuid(), code, "Label", _now).IsFailure);

    // Mutation failures must leave the aggregate unchanged.
    [Fact]
    public void Locale_rename_validates_bounds_without_changing_identity()
    {
        var locale = Locale.Create(Guid.NewGuid(), Guid.NewGuid(), "fa-IR", "Original", _now).Value;
        Assert.True(locale.Rename(" ").IsFailure);
        Assert.True(locale.Rename(new string('x', 101)).IsFailure);
        Assert.Equal("Original", locale.DisplayName);
        Assert.True(locale.Rename(new string('x', 100)).IsSuccess);
        Assert.Equal("fa-IR", locale.Code);
        Assert.True(Locale.Create(Guid.Empty, locale.TenantId, "fa", "Name", _now).IsFailure);
        Assert.True(Locale.Create(Guid.NewGuid(), Guid.Empty, "fa", "Name", _now).IsFailure);
        Assert.True(Locale.Create(Guid.NewGuid(), locale.TenantId, "fa", "Name", _now.ToOffset(TimeSpan.FromHours(1))).IsFailure);
    }
}
