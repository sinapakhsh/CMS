using CMS.Application.Identity;
using MediatR;

namespace CMS.Api.Endpoints;

// HTTP authentication contracts delegate all application operations to the mediator.
public static class IdentityEndpoints
{
    // Anonymous credential routes have stricter rate limits than authenticated API traffic.
    public static void MapIdentityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/identity");
        group.MapPost("/roles", async (CreateRoleCommand request, ISender sender, CancellationToken ct) =>
            ApiResult.From(await sender.Send(request, ct))).RequireAuthorization("identity.manage");
        group.MapPost("/roles/assign", async (AssignRoleCommand request, ISender sender, CancellationToken ct) =>
            ApiResult.From(await sender.Send(request, ct))).RequireAuthorization("identity.manage");
        group.MapPost("/register", async (RegisterCommand request, ISender sender, CancellationToken ct) =>
            ApiResult.From(await sender.Send(request, ct))).RequireRateLimiting("credentials");
        group.MapPost("/login", async (LoginCommand request, ISender sender, CancellationToken ct) =>
            ApiResult.From(await sender.Send(request, ct))).RequireRateLimiting("credentials");
        group.MapPost("/refresh", async (RefreshCommand request, ISender sender, CancellationToken ct) =>
            ApiResult.From(await sender.Send(request, ct))).RequireRateLimiting("credentials");
        group.MapPost("/logout", async (HttpContext context, ISender sender, CancellationToken ct) =>
            ApiResult.From(await sender.Send(new LogoutCommand(Guid.Parse(context.User.FindFirst("sid")!.Value)), ct)))
            .RequireAuthorization();
    }
}
