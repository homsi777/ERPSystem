using ERPSystem.Application.Results;

namespace ERPSystem.Api.Mapping;

public static class ApplicationResultHttpMapper
{
    public static IResult ToHttpResult<T>(
        ApplicationResult<T> result,
        Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
            return onSuccess?.Invoke(result.Value!) ?? Results.Ok(result.Value);

        return ToErrorResult(result);
    }

    public static IResult ToHttpResult(ApplicationResult result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        return ToErrorResult(result);
    }

    public static IResult ToUnauthorized(ApplicationResult result) =>
        Results.Json(CreateErrorBody(result), statusCode: StatusCodes.Status401Unauthorized);

    public static IResult ToUnauthorized<T>(ApplicationResult<T> result) =>
        Results.Json(CreateErrorBody(result), statusCode: StatusCodes.Status401Unauthorized);

    private static IResult ToErrorResult(ApplicationResult result) =>
        Results.Json(CreateErrorBody(result), statusCode: MapStatusCode(result.Status));

    private static int MapStatusCode(ApplicationResultStatus status) => status switch
    {
        ApplicationResultStatus.ValidationFailed => StatusCodes.Status400BadRequest,
        ApplicationResultStatus.NotFound => StatusCodes.Status404NotFound,
        ApplicationResultStatus.PermissionDenied => StatusCodes.Status403Forbidden,
        ApplicationResultStatus.Conflict => StatusCodes.Status409Conflict,
        ApplicationResultStatus.Failure => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError
    };

    private static ApiErrorResponse CreateErrorBody(ApplicationResult result) => new(
        result.Status.ToString(),
        result.ErrorMessage ?? "Request failed.",
        result.ValidationErrors);
}

public sealed record ApiErrorResponse(
    string Code,
    string Message,
    IReadOnlyList<ValidationError> ValidationErrors);
