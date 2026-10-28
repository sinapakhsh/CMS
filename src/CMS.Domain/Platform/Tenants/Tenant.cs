using System.Text.RegularExpressions;
using CMS.SharedKernel.Results;

namespace CMS.Domain.Platform.Tenants;

// A tenant owns its root record and all subordinate platform data.
public sealed class Tenant : TenantAggregate
{
    private Tenant(Guid id, Guid tenantId, string slug, string name) : base(id, tenantId)
    {
        Slug = slug;
        Name = name;
    }

    // Slugs provide a stable, normalized lookup key without exposing a display name.
    public string Slug { get; private set; }

    // Display names can be localized by later presentation layers.
    public string Name { get; private set; }

    // Invalid business input returns a result instead of constructing a broken tenant.
    public static Result<Tenant> Create(Guid id, string slug, string name, DateTimeOffset now)
    {
        if (id == Guid.Empty || now.Offset != TimeSpan.Zero ||
            string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(name))
            return Result<Tenant>.Failure(new Error("Tenant.Invalid", "Tenant identity, slug, name and UTC time are required."));

        slug = slug.Trim().ToLowerInvariant();
        name = name.Trim();
        if (slug.Length > 63 || !Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)) ||
            name.Length > 200)
            return Result<Tenant>.Failure(new Error("Tenant.Invalid", "Tenant slug or name is invalid."));

        var tenant = new Tenant(id, id, slug, name);
        tenant.RaiseDomainEvent(new TenantCreated(Guid.NewGuid(), now, id));
        return Result<Tenant>.Success(tenant);
    }
}
