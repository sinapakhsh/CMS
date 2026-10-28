using CMS.Application.Common.Abstractions;
using CMS.Domain.Content.ContentTypes;
using CMS.Domain.Content.Entries;
using CMS.Domain.Platform.Locales;
using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Content;

// Relationships are loaded through tenant-filtered repositories before accepting submitted identifiers.
public sealed class SaveEntryHandler(IRepository<Entry> entries, IRepository<ContentType> types, IRepository<Locale> locales,
    ICurrentTenant tenant, IIdGenerator ids, IClock clock) : IRequestHandler<SaveEntryCommand, Result<Guid>>
{
    // Optimistic concurrency rejects stale editors before changing the aggregate.
    public async Task<Result<Guid>> Handle(SaveEntryCommand request, CancellationToken cancellationToken)
    {
        var type = await types.GetByIdAsync(request.ContentTypeId, cancellationToken);
        var locale = await locales.GetByIdAsync(request.LocaleId, cancellationToken);
        if (type is null || locale is null) return Result<Guid>.Failure(new Error("Content.NotFound", "Content type or locale not found."));
        if (request.Id is null)
        {
            var created = Entry.Create(ids.NewId(), tenant.TenantId ?? Guid.Empty, type, locale.Id, request.Slug, request.Title, request.FieldsJson, clock.UtcNow);
            if (created.IsFailure) return Result<Guid>.Failure(created.Error);
            entries.Add(created.Value);
            return Result<Guid>.Success(created.Value.Id);
        }
        var entry = await entries.GetByIdAsync(request.Id.Value, cancellationToken);
        if (entry is null) return Result<Guid>.Failure(new Error("Entry.NotFound", "Entry not found."));
        if (Convert.ToBase64String(entry.RowVersion) != request.RowVersion)
            return Result<Guid>.Failure(new Error("Persistence.Concurrency", "Reload the entry before editing."));
        if (entry.LocaleId != locale.Id || entry.ContentTypeId != type.Id)
            return Result<Guid>.Failure(new Error("Entry.Invalid", "Entry type and locale cannot change."));
        var edited = entry.Edit(type, request.Slug, request.Title, request.FieldsJson, clock.UtcNow);
        return edited.IsSuccess ? Result<Guid>.Success(entry.Id) : Result<Guid>.Failure(edited.Error);
    }
}
