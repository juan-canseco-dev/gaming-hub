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
                return Results.BadRequest(result.Error);
            }
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .WithName(nameof(GetTotalUnreadMessagesCount))
        .WithTags(nameof(Chat));
    }
}
