using Carter;
using GameHub.Application.Features.Chats.Queries.GetMyChats;
using GameHub.Domain.Chats;
using MediatR;
using static GameHub.Application.Features.Chats.Queries.GetMyChats.GetUserChats;

namespace GameHub.WebAPI.Endpoints.Chats;

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
                : Results.BadRequest(result.Error);
        })
        .WithName(nameof(GetUserChats))
        .WithTags(nameof(Chat))
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization();
    }
}
