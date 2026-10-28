using CMS.Application.Common.Abstractions;

namespace CMS.Application.Identity;

// Tenant identity cannot be chosen through a role creation payload.
public sealed record CreateRoleCommand(string Name, string[] Permissions, string IdempotencyKey) : ICommand;
