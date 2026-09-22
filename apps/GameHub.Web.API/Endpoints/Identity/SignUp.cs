using Carter;
using GameHub.Application.Abstractions.Identity;
using GameHub.Contracts.Identity;

namespace GameHub.Web.API.Endpoints.Identity;

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
            : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        }
        )
        .AllowAnonymous()
        .ProducesValidationProblem()
        .Produces<Guid>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Register a user")
        .WithDescription("Creates an identity account and GameHub profile, then returns the new user identifier.")
        .WithName(nameof(SignUp))
        .WithTags("Auth");
    }
}
