using CMS.Application.Platform.Locales;
using MediatR;

namespace CMS.Api.Endpoints;

// Tenant selection comes exclusively from verified claims.
public static class PlatformEndpoints
{
    // Provisioning tenants remains a local administrative operation.
    public static void MapPlatformEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/platform/locales").RequireAuthorization("platform.manage");
        group.MapGet("/", async (ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(new ListLocalesQuery(), ct)));
        group.MapPost("/", async (CreateLocaleCommand request, ISender sender, CancellationToken ct) =>
            ApiResult.From(await sender.Send(request, ct)));
    }
}
