using Carter;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using MediatR;
using static GameHub.Application.Features.Chats.Queries.GetUnreadMessagesCount.GetUnreadMessagesCountByChat;

namespace GameHub.Web.API.Endpoints.Chats;

public class GetUnreadMessagesCount : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chats/{chatId:guid}/messages/unread/count", async (
         Guid chatId,
         IMediator mediator,
         CancellationToken cancellationToken
        ) =>
        {
            var query = new Query(chatId);
            var result = await mediator.Send(query, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error.Equals(ChatErrors.ChatGroupNotFound(chatId)))
                {
                    return result.Error.ToProblem(StatusCodes.Status404NotFound);
                }
                return result.Error.ToProblem(StatusCodes.Status400BadRequest);
            }
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .Produces<int>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get unread messages for a chat")
        .WithDescription("Returns the authenticated user's unread message count for one chat.")
        .WithName(nameof(GetUnreadMessagesCount))
        .WithTags(nameof(Chat));
    }
}
