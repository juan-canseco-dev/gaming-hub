using Carter;
using GameHub.Application.Abstractions.Identity;
using GameHub.Application.Contracts.Identity;

namespace GameHub.WebAPI.Endpoints.Identity;

public class SignIn : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/identity/auth", async (
                   IIdentityService service,
                   GetTokenRequest request,
                   CancellationToken cancellationToken) =>
        {
            var result = await service.GetTokenAsync(request, cancellationToken);
            return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error);
        }
        )
        .AllowAnonymous()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName(nameof(SignIn))
        .WithTags("Auth");
    }
}
