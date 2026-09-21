using GameHub.Abstractions.Primitives;
using GameHub.Web.API.ProblemDetails;
using Microsoft.AspNetCore.WebUtilities;

namespace GameHub.Web.API.Extensions;

public static class ErrorHttpResultExtensions
{
    public static IResult ToProblem(this Error error, int statusCode)
    {
        ArgumentNullException.ThrowIfNull(error);

        return Results.Problem(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = ProblemTypes.ForStatus(statusCode),
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Status = statusCode,
            Detail = error.Description,
            Extensions = { ["code"] = error.Code }
        });
    }
}
