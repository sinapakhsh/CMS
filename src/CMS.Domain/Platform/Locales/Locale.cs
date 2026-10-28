using System.Globalization;
using CMS.SharedKernel.Results;

namespace CMS.Domain.Platform.Locales;

// Locale is the single language catalogue for every module within a tenant.
public sealed class Locale : TenantAggregate
{
    private Locale(Guid id, Guid tenantId, string code, string displayName, bool isRightToLeft) : base(id, tenantId)
    {
        Code = code;
        DisplayName = displayName;
        IsRightToLeft = isRightToLeft;
    }

    // Culture names are normalized before uniqueness is enforced by SQL Server.
    public string Code { get; private set; }

    // Editors can choose a display label without changing the language identity.
    public string DisplayName { get; private set; }

    // Direction derives from the culture instead of a client-supplied flag.
    public bool IsRightToLeft { get; private set; }

    // Locale validation is shared with command validation to avoid conflicting rules.
    public static bool IsValidCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 35) return false;
        try
        {
            var culture = CultureInfo.GetCultureInfo(code);
            return culture.Name.Length > 0 && CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Any(known => string.Equals(known.Name, culture.Name, StringComparison.OrdinalIgnoreCase));
        }
        catch (CultureNotFoundException) { return false; }
    }

    // Expected input failures never leave partially initialized locale entities.
    public static Result<Locale> Create(Guid id, Guid tenantId, string code, string displayName, DateTimeOffset now)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || now.Offset != TimeSpan.Zero ||
            !IsValidCode(code) || string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 100)
            return Result<Locale>.Failure(new Error("Locale.Invalid", "Locale identity, culture, display name and UTC time are required."));

        var culture = CultureInfo.GetCultureInfo(code);
        var locale = new Locale(id, tenantId, culture.Name, displayName.Trim(), culture.TextInfo.IsRightToLeft);
        locale.RaiseDomainEvent(new LocaleCreated(Guid.NewGuid(), now, tenantId, id, locale.Code));
        return Result<Locale>.Success(locale);
    }

    // A label change must preserve the culture and tenant identity.
    public Result Rename(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 100)
            return Result.Failure(new Error("Locale.InvalidName", "The display name must contain 1 to 100 characters."));
        DisplayName = displayName.Trim();
        return Result.Success();
    }
}
