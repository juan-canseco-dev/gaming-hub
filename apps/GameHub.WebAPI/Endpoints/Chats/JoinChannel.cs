using Carter;
using GameHub.Application.Features.Chats.Commands.Join;
using GameHub.Domain.Chats;
using MediatR;

namespace GameHub.WebAPI.Endpoints.Chats;

public class JoinChannel : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/channels/join", async (
                   IMediator mediator,
                   JoinChat.Command command,
                   CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error.Equals(ChatErrors.ChatGroupNotFound(command.ChatId)))
                {
                    return Results.NotFound(result.Error);
                }
                return Results.BadRequest(result.Error);
            }

            return Results.Ok();
        }
        )
        .RequireAuthorization()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithName(nameof(SendMessage))
        .WithTags(nameof(Chat));
    }
}
