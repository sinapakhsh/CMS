using CMS.Application.Common.Abstractions;

namespace CMS.Application.Content;

// Restore is a new revision guarded by the currently observed row version.
public sealed record RestoreEntryCommand(Guid Id, int Version, string RowVersion, string IdempotencyKey) : ICommand;
