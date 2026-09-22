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
                   JoinChatRequest request,
                   CancellationToken cancellationToken) =>
        {
            var command = new JoinChatCommand.Command(request.ChatId);
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
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Join a chat")
        .WithDescription("Adds the authenticated user to the chat associated with a channel.")
        .WithName(nameof(JoinChat))
        .WithTags(nameof(Chat));
    }

    internal sealed record JoinChatRequest(Guid ChatId);
}
