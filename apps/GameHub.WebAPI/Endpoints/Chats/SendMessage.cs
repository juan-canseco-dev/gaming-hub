using Carter;
using GameHub.Application.Features.Chats.Commands.SendMessage;
using GameHub.Domain.Chats;
using MediatR;

namespace GameHub.WebAPI.Endpoints.Chats;

public class SendMessage : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chats/messages", async (
                   IMediator mediator,
                   ChatSendMessage.Command command,
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
            return Results.Ok(result.Value);
        }
        )
        .RequireAuthorization()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status401Unauthorized)
        .WithName(nameof(JoinChannel))
        .WithTags(nameof(Chat));
    }
}
