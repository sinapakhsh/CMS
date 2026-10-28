using CMS.Application.Common.Abstractions;

namespace CMS.Application.Content;

// Updates require the last observed row version; creates omit both identity and version.
public sealed record SaveEntryCommand(Guid? Id, Guid ContentTypeId, Guid LocaleId, string Slug, string Title,
    string FieldsJson, string? RowVersion, string IdempotencyKey) : ICommand;
