using CMS.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace CMS.Infrastructure.Platform;

// EF's GUID generator follows SQL Server ordering instead of UUID byte ordering.
public sealed class SqlServerIdGenerator : IIdGenerator
{
    private readonly SequentialGuidValueGenerator _generator = new();

    // The generator is thread-safe and independent of the current entity entry.
    public Guid NewId() => _generator.Next(null!);
}
