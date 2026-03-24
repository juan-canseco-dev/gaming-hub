using Carter;
using GameHub.Application.Abstractions.Identity;
using GameHub.Contracts.Identity;

namespace GameHub.WebAPI.Endpoints.Identity;

public class SignUp : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/identity/auth/register", async (
                   IIdentityService service,
                   RegisterUserRequest request,
                   CancellationToken cancellationToken) =>
        {
            var result = await service.RegisterAsync(request, cancellationToken);
            return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.Error);
        }
        )
        .AllowAnonymous()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName(nameof(SignUp))
        .WithTags("Auth");
    }
}
