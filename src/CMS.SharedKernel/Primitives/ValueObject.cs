namespace CMS.SharedKernel.Primitives;

// Equality depends on values and runtime type rather than persistence identity.
public abstract class ValueObject : IEquatable<ValueObject>
{
    // Derived types list immutable components in a deterministic order.
    protected abstract IEnumerable<object?> GetEqualityComponents();

    // Different concepts must not compare equal merely because their fields match.
    public bool Equals(ValueObject? other) =>
        other is not null && GetType() == other.GetType()
        && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    // Object-based collections use the same value semantics as typed callers.
    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    // Include the runtime type and the same ordered components used by equality.
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(GetType());
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }

    // Operators preserve equality semantics when either operand is null.
    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    // Inequality delegates to equality so the operators cannot disagree.
    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
