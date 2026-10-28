namespace CMS.Domain.Content.Entries;

// Revision rows are immutable children of their owning entry.
public sealed record EntryRevision(Guid Id, Guid TenantId, Guid EntryId, int Version, string Slug, string Title, string FieldsJson, DateTimeOffset CreatedAtUtc);
