using CMS.Application.Content;
using MediatR;

namespace CMS.Api.Endpoints;

// Management writes and anonymous published reads use separate routes and authorization policies.
public static class ContentEndpoints
{
    // Every management operation passes through the application mediator.
    public static void MapContentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/content").RequireAuthorization("content.edit");
        group.MapPost("/types", async (CreateContentTypeCommand request, ISender sender, CancellationToken ct) => ApiResult.From(await sender.Send(request, ct)));
        group.MapPost("/entries", async (SaveEntryCommand request, HttpContext context, ISender sender, CancellationToken ct) =>
        {
            if (request.Id is not null)
            {
                if (!context.Request.Headers.TryGetValue("If-Match", out var etag)) return Results.Problem(statusCode: 428, title: "If-Match is required.");
                request = request with { RowVersion = etag.ToString().Trim('"') };
            }
            return ApiResult.From(await sender.Send(request, ct));
        });
        group.MapPost("/entries/transition", async (TransitionEntryCommand request, HttpContext context, ISender sender, CancellationToken ct) =>
        {
            if (!context.Request.Headers.TryGetValue("If-Match", out var etag)) return Results.Problem(statusCode: 428, title: "If-Match is required.");
            return ApiResult.From(await sender.Send(request with { RowVersion = etag.ToString().Trim('"') }, ct));
        }).RequireAuthorization("content.publish");
        group.MapGet("/entries", async (int? offset, int? limit, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new ListEntriesQuery(offset ?? 0, limit ?? 50), ct)));
        group.MapPost("/entries/restore", async (RestoreEntryCommand request, HttpContext context, ISender sender, CancellationToken ct) =>
        {
            if (!context.Request.Headers.TryGetValue("If-Match", out var etag)) return Results.Problem(statusCode: 428, title: "If-Match is required.");
            return ApiResult.From(await sender.Send(request with { RowVersion = etag.ToString().Trim('"') }, ct));
        });
        app.MapGet("/api/v1/public/{tenant}/{locale}/entries/{slug}", async (string tenant, string locale, string slug,
            HttpContext context, ISender sender, CancellationToken ct) =>
        {
            var entry = await sender.Send(new PublicEntryQuery(tenant, locale, slug), ct);
            if (entry is null) return Results.NotFound();
            if (!string.Equals(entry.Slug, slug, StringComparison.Ordinal))
                return Results.Redirect($"/api/v1/public/{Uri.EscapeDataString(tenant)}/{Uri.EscapeDataString(locale)}/entries/{Uri.EscapeDataString(entry.Slug)}", permanent: true);
            context.Response.Headers.ETag = '"' + Convert.ToBase64String(entry.RowVersion) + '"';
            return Results.Ok(entry);
        });
    }
}
