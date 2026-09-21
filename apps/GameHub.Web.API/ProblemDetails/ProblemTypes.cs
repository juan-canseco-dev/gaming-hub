namespace GameHub.Web.API.ProblemDetails;

internal static class ProblemTypes
{
    private const string BaseUri = "https://api.gamehub.example/problems/";

    internal const string Validation = BaseUri + "validation-error";
    internal const string BadRequest = BaseUri + "bad-request";
    internal const string NotFound = BaseUri + "not-found";
    internal const string Unauthorized = BaseUri + "unauthorized";
    internal const string Forbidden = BaseUri + "forbidden";
    internal const string DatabaseUnavailable = BaseUri + "database-unavailable";
    internal const string InternalServerError = BaseUri + "internal-server-error";

    internal static string ForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => BadRequest,
        StatusCodes.Status401Unauthorized => Unauthorized,
        StatusCodes.Status403Forbidden => Forbidden,
        StatusCodes.Status404NotFound => NotFound,
        StatusCodes.Status503ServiceUnavailable => DatabaseUnavailable,
        StatusCodes.Status500InternalServerError => InternalServerError,
        _ => BaseUri + "http-error"
    };
}
