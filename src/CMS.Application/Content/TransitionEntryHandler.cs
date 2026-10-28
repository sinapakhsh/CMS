using CMS.Application.Common.Abstractions;
using CMS.Domain.Content.Entries;
using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Content;

// Publication state is changed only through the domain state machine.
public sealed class TransitionEntryHandler(IRepository<Entry> repository) : IRequestHandler<TransitionEntryCommand, Result<Guid>>
{
    // SQL rowversion remains the final guard against a concurrent update after this check.
    public async Task<Result<Guid>> Handle(TransitionEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entry is null) return Result<Guid>.Failure(new Error("Entry.NotFound", "Entry not found."));
        if (Convert.ToBase64String(entry.RowVersion) != request.RowVersion)
            return Result<Guid>.Failure(new Error("Persistence.Concurrency", "Reload the entry before changing its state."));
        var result = entry.Transition(request.Status, request.PublishAtUtc);
        return result.IsSuccess ? Result<Guid>.Success(entry.Id) : Result<Guid>.Failure(result.Error);
    }
}
