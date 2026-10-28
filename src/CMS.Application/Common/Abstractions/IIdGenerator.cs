namespace CMS.Application.Common.Abstractions;

// Storage-specific identifier ordering stays outside the domain layer.
public interface IIdGenerator
{
    // SQL Server implementations supply sequential GUIDs.
    Guid NewId();
}
