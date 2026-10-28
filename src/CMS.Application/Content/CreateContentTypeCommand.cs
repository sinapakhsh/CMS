using CMS.Application.Common.Abstractions;

namespace CMS.Application.Content;

// Type definitions are tenant-scoped write operations with replay protection.
public sealed record CreateContentTypeCommand(string Key, string Name, string FieldsJson, string IdempotencyKey) : ICommand;
