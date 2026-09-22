using Carter;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using MediatR;
using static GameHub.Application.Features.Chats.Queries.GetTotalUnreadMessagesCount.GetTotalChatUnreadMessagesCount;

namespace GameHub.Web.API.Endpoints.Chats;

public class GetTotalUnreadMessagesCount : ICarterModule
{

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chats/unread-count", async (
         IMediator mediator,
         CancellationToken cancellationToken
        ) =>
        {
            var query = new Query();
            var result = await mediator.Send(query, cancellationToken);
            if (result.IsFailure)
            {
                return result.Error.ToProblem(StatusCodes.Status400BadRequest);
            }
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .Produces<int>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Get total unread messages")
        .WithDescription("Returns the authenticated user's unread message count across all joined chats.")
        .WithName(nameof(GetTotalUnreadMessagesCount))
        .WithTags(nameof(Chat));
    }
}
