using System.Text.Json;
using System.Text.RegularExpressions;
using CMS.Domain.Platform;
using CMS.SharedKernel.Results;

namespace CMS.Domain.Content.ContentTypes;

// Field definitions are bounded data, never executable expressions or arbitrary CLR types.
public sealed class ContentType : TenantAggregate
{
    private ContentType(Guid id, Guid tenantId, string key, string name, string fieldsJson) : base(id, tenantId)
    { Key = key; Name = name; FieldsJson = fieldsJson; }
    // Stable keys identify types independently from their translated display names.
    public string Key { get; private set; }
    // Display names are constrained to match SQL storage limits.
    public string Name { get; private set; }
    // Definitions use a JSON object mapping field names to type and required flags.
    public string FieldsJson { get; private set; }
    // The schema is immutable once created so older entries never silently change meaning.
    public static Result<ContentType> Create(Guid id, Guid tenantId, string key, string name, string fieldsJson)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || !ValidKey(key) || string.IsNullOrWhiteSpace(name)
            || name.Length > 200 || !ValidSchema(fieldsJson))
            return Result<ContentType>.Failure(new Error("ContentType.Invalid", "The content type or its field definitions are invalid."));
        return Result<ContentType>.Success(new ContentType(id, tenantId, key, name.Trim(), fieldsJson));
    }
    // Unknown fields and incompatible JSON values are rejected at the aggregate boundary.
    public bool Accepts(string fieldsJson)
    {
        if (string.IsNullOrWhiteSpace(fieldsJson) || fieldsJson.Length > 262144) return false;
        try
        {
            using var schema = JsonDocument.Parse(FieldsJson);
            using var data = JsonDocument.Parse(fieldsJson);
            if (data.RootElement.ValueKind != JsonValueKind.Object) return false;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in data.RootElement.EnumerateObject())
                if (!names.Add(field.Name) || !schema.RootElement.TryGetProperty(field.Name, out _)) return false;
            foreach (var field in schema.RootElement.EnumerateObject())
            {
                var required = field.Value.TryGetProperty("required", out var flag) && flag.GetBoolean();
                if (!data.RootElement.TryGetProperty(field.Name, out var value) || value.ValueKind == JsonValueKind.Null)
                { if (required) return false; else continue; }
                var valid = field.Value.GetProperty("type").GetString() switch
                {
                    "text" => value.ValueKind == JsonValueKind.String && (!required || !string.IsNullOrWhiteSpace(value.GetString())),
                    "number" => value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out _),
                    "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                    "date" => value.ValueKind == JsonValueKind.String && value.TryGetDateTimeOffset(out _),
                    "media" or "reference" => value.ValueKind == JsonValueKind.String && value.TryGetGuid(out var reference) && reference != Guid.Empty,
                    "group" => value.ValueKind == JsonValueKind.Array,
                    "json" => value.ValueKind is JsonValueKind.Object or JsonValueKind.Array,
                    _ => false
                };
                if (!valid) return false;
            }
            return true;
        }
        catch (JsonException) { return false; }
    }
    private static bool ValidSchema(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 32768) return false;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in document.RootElement.EnumerateObject())
            {
                if (!ValidKey(field.Name) || !names.Add(field.Name) || names.Count > 100 || field.Value.ValueKind != JsonValueKind.Object
                    || !field.Value.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String
                    || type.GetString() is not ("text" or "number" or "boolean" or "date" or "media" or "reference" or "group" or "json")) return false;
                if (field.Value.TryGetProperty("required", out var required) && required.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
            }
            return true;
        }
        catch (JsonException) { return false; }
    }
    private static bool ValidKey(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 63
        && Regex.IsMatch(value, "^[a-z][a-z0-9_-]*$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
}
