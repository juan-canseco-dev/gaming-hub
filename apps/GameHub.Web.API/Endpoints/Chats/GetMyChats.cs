using Carter;
using GameHub.Application.Features.Chats.Queries.GetMyChats;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using MediatR;
using static GameHub.Application.Features.Chats.Queries.GetMyChats.GetUserChats;
using GameHub.Contracts.Chats;

namespace GameHub.Web.API.Endpoints.Chats;

public class GetMyChats : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chats", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new Query(), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.Error.ToProblem(StatusCodes.Status400BadRequest);
        })
        .WithName(nameof(GetUserChats))
        .WithTags(nameof(Chat))
        .WithSummary("List my chats")
        .WithDescription("Returns the chats joined by the authenticated user, including unread counts and last-message previews.")
        .Produces<IReadOnlyCollection<ChatDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireAuthorization();
    }
}
