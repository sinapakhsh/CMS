using CMS.SharedKernel.Results;

namespace CMS.Application.Common.Abstractions;

// Commands, audit, outbox and replay results share one SQL transaction.
public interface IUnitOfWork
{
    // Nested command behaviors join an existing transaction rather than committing early.
    Task<Result<Guid>> ExecuteAsync(Func<Task<Result<Guid>>> action, CancellationToken cancellationToken);

    // A durable, tenant-scoped replay record is committed together with the command.
    Task<Result<Guid>> ExecuteIdempotentAsync(string operation, string key, string fingerprint,
        Func<Task<Result<Guid>>> action, CancellationToken cancellationToken);
}
