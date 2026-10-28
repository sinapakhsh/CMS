using CMS.SharedKernel.Primitives;

namespace CMS.SharedKernel.UnitTests;

// Guard collection semantics for domain values before introducing feature models.
public sealed class ValueObjectTests
{
    // Equivalent values must behave as one key in hashed collections.
    [Fact]
    public void Equivalent_values_share_equality_and_hashing()
    {
        var first = new SampleValue("fa", 1);
        var second = new SampleValue("fa", 1);
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Single(new HashSet<ValueObject> { first, second });
    }

    // Component order, runtime type and nulls all participate in equality.
    [Fact]
    public void Different_components_types_and_nulls_are_not_equal()
    {
        ValueObject first = new SampleValue("fa", 1);
        Assert.NotEqual(first, new SampleValue("en", 1));
        Assert.NotEqual(first, new SampleValue("fa", 2));
        Assert.NotEqual(first, new DifferentValue("fa", 1));
        Assert.False(first.Equals(null));
        Assert.False(first.Equals("fa"));
        Assert.False(first == null);
        Assert.False(null == first);
        ValueObject? missing = null;
        Assert.True(missing == null);
        Assert.True(first != missing);
        Assert.Equal(new SampleValue(null, 0), new SampleValue(null, 0));
    }

    private sealed class SampleValue(string? text, int number) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents() => [text, number];
    }

    private sealed class DifferentValue(string? text, int number) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents() => [text, number];
    }
}
