using MediatR;

namespace CMS.Application.Content;

// Public routes select an existing tenant by slug without trusting administrative tenant headers.
public sealed record PublicEntryQuery(string Tenant, string Locale, string Slug) : IRequest<EntryDto?>;
