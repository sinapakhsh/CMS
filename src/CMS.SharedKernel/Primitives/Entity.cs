namespace CMS.SharedKernel.Primitives;

// Identity remains stable for the lifetime of an entity, including before persistence.
public abstract class Entity<TId> where TId : notnull
{
    // Default identifiers cannot distinguish independently created entities.
    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (EqualityComparer<TId>.Default.Equals(id, default))
        {
            throw new ArgumentException("An entity must have a non-default identifier.", nameof(id));
        }

        Id = id;
    }

    // Derived entities cannot change identity during an update.
    public TId Id { get; }
}
