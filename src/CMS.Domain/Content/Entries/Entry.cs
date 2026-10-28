using System.Text.RegularExpressions;
using CMS.Domain.Content.ContentTypes;
using CMS.Domain.Platform;
using CMS.SharedKernel.Results;

namespace CMS.Domain.Content.Entries;

// Each locale owns an entry with immutable revision snapshots and explicit publication state.
public sealed class Entry : TenantAggregate
{
    private readonly List<EntryRevision> _revisions = [];
    private readonly List<EntryRedirect> _redirects = [];
    private Entry(Guid id, Guid tenantId, Guid contentTypeId, Guid localeId, string slug, string title, string fieldsJson) : base(id, tenantId)
    { ContentTypeId = contentTypeId; LocaleId = localeId; Slug = slug; Title = title; FieldsJson = fieldsJson; }
    // Type and locale remain stable across edits and revisions.
    public Guid ContentTypeId { get; private set; }
    // Locale identity references the tenant's single language catalogue.
    public Guid LocaleId { get; private set; }
    // Slug uniqueness is enforced per tenant and locale.
    public string Slug { get; private set; }
    // Titles are plain data; rendering clients must encode them.
    public string Title { get; private set; }
    // Structured content is validated against its type before each write.
    public string FieldsJson { get; private set; }
    // State transitions prevent publishing directly from an unreviewed draft.
    public string Status { get; private set; } = "Draft";
    // A future date prevents public visibility until the schedule is due.
    public DateTimeOffset? PublishAtUtc { get; private set; }
    // Public consumers receive read-only snapshots, never mutable revision lists.
    public IReadOnlyCollection<EntryRevision> Revisions => _revisions.AsReadOnly();
    // Redirects retain previous routes when a slug changes.
    public IReadOnlyCollection<EntryRedirect> Redirects => _redirects.AsReadOnly();
    // Creation captures version one so every persisted state has a recoverable snapshot.
    public static Result<Entry> Create(Guid id, Guid tenantId, ContentType type, Guid localeId, string slug, string title, string fieldsJson, DateTimeOffset now)
    {
        if (id == Guid.Empty || tenantId != type.TenantId || localeId == Guid.Empty || !ValidText(slug, title) || !type.Accepts(fieldsJson))
            return Result<Entry>.Failure(new Error("Entry.Invalid", "Entry fields, title, slug or ownership are invalid."));
        var entry = new Entry(id, tenantId, type.Id, localeId, slug, title.Trim(), fieldsJson);
        entry.Snapshot(now);
        return Result<Entry>.Success(entry);
    }
    // Editing a published entry returns it to draft and preserves its previous snapshot.
    public Result Edit(ContentType type, string slug, string title, string fieldsJson, DateTimeOffset now)
    {
        if (type.Id != ContentTypeId || type.TenantId != TenantId || !ValidText(slug, title) || !type.Accepts(fieldsJson))
            return Result.Failure(new Error("Entry.Invalid", "Entry fields, title or slug are invalid."));
        if (Slug != slug)
        {
            if (_redirects.All(redirect => redirect.Slug != Slug))
                _redirects.Add(new EntryRedirect(Guid.NewGuid(), TenantId, Id, LocaleId, Slug));
        }
        Slug = slug; Title = title.Trim(); FieldsJson = fieldsJson; Status = "Draft"; PublishAtUtc = null;
        Snapshot(now);
        return Result.Success();
    }
    // Restoring is a new edit, preserving the full history instead of deleting intervening versions.
    public Result Restore(ContentType type, int version, DateTimeOffset now)
    {
        var revision = _revisions.SingleOrDefault(row => row.Version == version);
        return revision is null ? Result.Failure(new Error("Revision.NotFound", "Revision not found."))
            : Edit(type, revision.Slug, revision.Title, revision.FieldsJson, now);
    }
    // Publication accepts only reviewed content and UTC schedule values.
    public Result Transition(string status, DateTimeOffset? publishAtUtc)
    {
        var allowed = (Status, status) is ("Draft", "Review") or ("Review", "Published") or ("Review", "Draft")
            or ("Published", "Archived") or ("Archived", "Draft");
        if (!allowed || publishAtUtc?.Offset is { } offset && offset != TimeSpan.Zero)
            return Result.Failure(new Error("Entry.InvalidTransition", "The publication transition or UTC schedule is invalid."));
        Status = status;
        PublishAtUtc = status == "Published" ? publishAtUtc : null;
        return Result.Success();
    }
    private void Snapshot(DateTimeOffset now) => _revisions.Add(new EntryRevision(Guid.NewGuid(), TenantId, Id,
        _revisions.Count + 1, Slug, Title, FieldsJson, now));
    private static bool ValidText(string slug, string title) => !string.IsNullOrWhiteSpace(slug) && slug.Length <= 200
        && Regex.IsMatch(slug, "^[\\p{L}\\p{N}]+(?:-[\\p{L}\\p{N}]+)*$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100))
        && !string.IsNullOrWhiteSpace(title) && title.Length <= 300;
}
