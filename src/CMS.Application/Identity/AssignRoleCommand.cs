using CMS.Application.Common.Abstractions;

namespace CMS.Application.Identity;

// Role assignments require replay protection and a trusted administrative caller.
public sealed record AssignRoleCommand(Guid UserId, Guid RoleId, string IdempotencyKey) : ICommand;
