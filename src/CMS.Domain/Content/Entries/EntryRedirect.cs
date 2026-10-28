namespace CMS.Domain.Content.Entries;

// Previous slugs point to an entry identity so subsequent renames do not form redirect chains.
public sealed record EntryRedirect(Guid Id, Guid TenantId, Guid EntryId, Guid LocaleId, string Slug);
