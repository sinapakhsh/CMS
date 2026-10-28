using CMS.SharedKernel.Results;

namespace CMS.Api.Endpoints;

// Stable business codes complement HTTP status without exposing persistence exceptions.
public static class ApiResult
{
    // Known failures become RFC 9457 responses; successful payloads retain their concrete shape.
    public static IResult From<T>(Result<T> result) => result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(
        statusCode: result.Error.Code switch
        {
            "Identity.InvalidCredentials" => 401,
            "Persistence.Concurrency" => 412,
            "Idempotency.Busy" => 409,
            _ when result.Error.Code.EndsWith("NotFound", StringComparison.Ordinal) => 404,
            _ when result.Error.Code.EndsWith("Conflict", StringComparison.Ordinal) || result.Error.Code.EndsWith("Exists", StringComparison.Ordinal) => 409,
            _ => 400
        }, title: result.Error.Description, extensions: new Dictionary<string, object?> { ["code"] = result.Error.Code });
}
