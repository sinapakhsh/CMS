using CMS.Application.Common.Abstractions;

namespace CMS.Application.Content;

// Workflow changes use the same concurrency and replay contract as content edits.
public sealed record TransitionEntryCommand(Guid Id, string Status, DateTimeOffset? PublishAtUtc, string RowVersion, string IdempotencyKey) : ICommand;
