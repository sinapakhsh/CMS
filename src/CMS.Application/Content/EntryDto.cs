namespace CMS.Application.Content;

// Row versions are opaque tokens used by management clients for conditional edits.
public sealed record EntryDto(Guid Id, Guid ContentTypeId, Guid LocaleId, string Slug, string Title, string FieldsJson,
    string Status, DateTimeOffset? PublishAtUtc, byte[] RowVersion);
