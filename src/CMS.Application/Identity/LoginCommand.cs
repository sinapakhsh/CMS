using CMS.SharedKernel.Results;
using MediatR;

namespace CMS.Application.Identity;

// Credentials stay within the identity boundary and are never included in command logging.
public sealed record LoginCommand(string Tenant, string Email, string Password) : IRequest<Result<TokenPair>>;
