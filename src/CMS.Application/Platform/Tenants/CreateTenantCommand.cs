using CMS.Application.Common.Abstractions;

namespace CMS.Application.Platform.Tenants;

// Provisioning is available only to trusted administrative callers until identity is implemented.
public sealed record CreateTenantCommand(string Slug, string Name, string IdempotencyKey) : ICommand;
