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
                    return Results.NotFound(result.Error);
                }
                return Results.BadRequest(result.Error);
            }

            return Results.Ok();
        })
        .RequireAuthorization()
        .WithName(nameof(MarkMessagesAsRead))
        .WithTags(nameof(Chat));
    }
}
