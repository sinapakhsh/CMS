namespace CMS.Application.Common.Abstractions;

// Audit records identify an actor without coupling application code to HTTP.
public interface ICurrentUser
{
    // Background and administrative operations may have no human actor.
    Guid? UserId { get; }
}
