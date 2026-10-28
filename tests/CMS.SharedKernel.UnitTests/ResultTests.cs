using CMS.SharedKernel.Results;

namespace CMS.SharedKernel.UnitTests;

// Ensure invalid outcomes cannot silently cross application boundaries.
public sealed class ResultTests
{
    // Commands with no payload still distinguish success from a failure.
    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    // Error identity must survive both typed and untyped result construction.
    [Fact]
    public void Failure_preserves_the_error()
    {
        var error = new Error("Content.NotFound", "The entry does not exist.");
        var result = Result.Failure(error);
        var typed = Result<string>.Failure(error);
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
        Assert.Same(error, typed.Error);
        Assert.Throws<InvalidOperationException>(() => typed.Value);
    }

    // Empty failures would erase the reason a command was rejected.
    [Fact]
    public void Failure_rejects_empty_and_null_errors()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
        Assert.Throws<ArgumentException>(() => Result<int>.Failure(Error.None));
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result<int>.Failure(null!));
    }

    // Value-type defaults are valid data, unlike null reference payloads.
    [Fact]
    public void Typed_success_preserves_zero_and_reference_values()
    {
        Assert.Equal(0, Result<int>.Success(0).Value);
        var value = new object();
        Assert.Same(value, Result<object>.Success(value).Value);
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }

    // Stable codes and useful descriptions are required for every real error.
    [Theory]
    [InlineData(null, "Description")]
    [InlineData("", "Description")]
    [InlineData(" ", "Description")]
    [InlineData("Code", null)]
    [InlineData("Code", "")]
    [InlineData("Code", " ")]
    public void Error_rejects_missing_code_or_description(string? code, string? description)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Error(code!, description!));
    }

    // Equivalent error descriptions should not require reference identity.
    [Fact]
    public void Errors_compare_by_value()
    {
        var first = new Error("Example.Failure", "Failure description.");
        var second = new Error("Example.Failure", "Failure description.");
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new Error("Example.Other", "Failure description."));
    }
}
