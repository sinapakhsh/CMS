using CMS.Application.Common.Behaviors;
using CMS.Application.Platform.Locales;
using CMS.Application.Platform.Tenants;
using CMS.SharedKernel.Results;

namespace CMS.Application.UnitTests;

// Boundary validation must reject oversized data before any handler or transaction runs.
public sealed class ValidationTests
{
    // An invalid command short-circuits the remainder of the pipeline.
    [Fact]
    public async Task Validation_prevents_handler_execution()
    {
        var reached = false;
        var behavior = new ValidationBehavior<CreateLocaleCommand>([new CreateLocaleValidator()]);
        var result = await behavior.Handle(new("fa-IR", " ", "key"),
            _ => { reached = true; return Task.FromResult(Result<Guid>.Success(Guid.NewGuid())); }, default);
        Assert.Equal("Validation.Failed", result.Error.Code);
        Assert.False(reached);
    }

    // Length limits align with the persisted schema and include both boundary cases.
    [Theory]
    [InlineData(100, 128, true)]
    [InlineData(101, 128, false)]
    [InlineData(100, 129, false)]
    [InlineData(0, 10, false)]
    [InlineData(10, 0, false)]
    public void Locale_validator_checks_exact_bounds(int nameLength, int keyLength, bool valid) =>
        Assert.Equal(valid, new CreateLocaleValidator().Validate(new CreateLocaleCommand("fa", new string('a', nameLength), new string('k', keyLength))).IsValid);

    // Slug and display-name bounds protect tenant provisioning as well.
    [Theory]
    [InlineData(63, 200, true)]
    [InlineData(64, 200, false)]
    [InlineData(63, 201, false)]
    [InlineData(0, 10, false)]
    public void Tenant_validator_checks_exact_bounds(int slugLength, int nameLength, bool valid) =>
        Assert.Equal(valid, new CreateTenantValidator().Validate(new CreateTenantCommand(new string('a', slugLength), new string('n', nameLength), "key")).IsValid);
}
