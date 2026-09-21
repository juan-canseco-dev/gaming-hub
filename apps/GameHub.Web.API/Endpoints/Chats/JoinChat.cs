using Carter;
using GameHub.Domain.Chats;
using MediatR;
using JoinChatCommand = GameHub.Application.Features.Chats.Commands.Join.JoinChat;

namespace GameHub.Web.API.Endpoints.Chats;

public class JoinChat : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/channels/join", async (
                   IMediator mediator,
                   JoinChatCommand.Command command,
                   CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error.Equals(ChatErrors.ChatGroupNotFound(command.ChatId)))
                {
                    return result.Error.ToProblem(StatusCodes.Status404NotFound);
                }
                return result.Error.ToProblem(StatusCodes.Status400BadRequest);
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
        .WithName(nameof(JoinChat))
        .WithTags(nameof(Chat));
    }
}
