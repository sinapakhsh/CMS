using CMS.Application.Common.Abstractions;
using CMS.Domain.Content.ContentTypes;
using CMS.Domain.Content.Entries;
using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Content;

// Restored data is revalidated against the immutable type definition.
public sealed class RestoreEntryHandler(IRepository<Entry> entries, IRepository<ContentType> types, IClock clock)
    : IRequestHandler<RestoreEntryCommand, Result<Guid>>
{
    // An unavailable historical version cannot silently restore another snapshot.
    public async Task<Result<Guid>> Handle(RestoreEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await entries.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null) return Result<Guid>.Failure(new Error("Entry.NotFound", "Entry not found."));
        if (Convert.ToBase64String(entry.RowVersion) != request.RowVersion)
            return Result<Guid>.Failure(new Error("Persistence.Concurrency", "Reload the entry before restoring."));
        var type = await types.GetByIdAsync(entry.ContentTypeId, cancellationToken);
        if (type is null) return Result<Guid>.Failure(new Error("ContentType.NotFound", "Content type not found."));
        var result = entry.Restore(type, request.Version, clock.UtcNow);
        return result.IsSuccess ? Result<Guid>.Success(entry.Id) : Result<Guid>.Failure(result.Error);
    }
}
