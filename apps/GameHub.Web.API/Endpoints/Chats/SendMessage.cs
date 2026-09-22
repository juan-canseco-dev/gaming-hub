using Carter;
using GameHub.Application.Features.Chats.Commands.SendMessage;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using MediatR;
using GameHub.Contracts.Chats;

namespace GameHub.Web.API.Endpoints.Chats;

public class SendMessage : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chats/messages", async (
                   IMediator mediator,
                   SendMessageRequest request,
                   CancellationToken cancellationToken) =>
        {
            var command = new ChatSendMessage.Command(request.ChatId, request.Content);
            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error.Equals(ChatErrors.ChatGroupNotFound(command.ChatId)))
                {
                    return result.Error.ToProblem(StatusCodes.Status404NotFound);
                }
                return result.Error.ToProblem(StatusCodes.Status400BadRequest);
            }
            return Results.Ok(result.Value);
        }
        )
        .RequireAuthorization()
        .ProducesValidationProblem()
        .Produces<MessageDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Send a chat message")
        .WithDescription("Creates a message in a chat joined by the authenticated user. Content is limited to 2,000 characters.")
        .WithName(nameof(SendMessage))
        .WithTags(nameof(Chat));
    }

    internal sealed record SendMessageRequest(Guid ChatId, string Content);
}
