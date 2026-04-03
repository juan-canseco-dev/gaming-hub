using Carter;
using GameHub.Domain.Chats;
using MediatR;
using static GameHub.Application.Features.Chats.Queries.GetUnreadMessagesCount.GetUnreadMessagesCountByChat;

namespace GameHub.WebAPI.Endpoints.Chats;

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
                    return Results.NotFound(result.Error);
                }
                return Results.BadRequest(result.Error);
            }
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName(nameof(GetUnreadMessagesCount))
        .WithTags(nameof(Chat));
    }
}
