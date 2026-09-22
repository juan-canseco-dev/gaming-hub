using Carter;
using GameHub.Application.Features.Chats.Commands.MarkAsRead;
using GameHub.Domain.Chats;
using MediatR;

namespace GameHub.Web.API.Endpoints.Chats;

public class MarkMessagesAsRead : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chats/{chatId:guid}/read", async (
            Guid chatId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new MarkChatAsRead.Command(chatId),
                cancellationToken);

            if (result.IsFailure)
            {
                if (result.Error.Equals(ChatErrors.ChatGroupNotFound(chatId)))
                {
                    return result.Error.ToProblem(StatusCodes.Status404NotFound);
                }
                return result.Error.ToProblem(StatusCodes.Status400BadRequest);
            }

            return Results.Ok();
        })
        .RequireAuthorization()
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Mark a chat as read")
        .WithDescription("Advances the authenticated user's read position to the latest message in the chat.")
        .WithName(nameof(MarkMessagesAsRead))
        .WithTags(nameof(Chat));
    }
}
