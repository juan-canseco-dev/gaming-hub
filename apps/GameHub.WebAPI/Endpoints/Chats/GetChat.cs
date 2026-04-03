using Carter;
using GameHub.Application.Features.Chats.Queries.GetById;
using GameHub.Domain.Chats;
using MediatR;
using static GameHub.Application.Features.Chats.Queries.GetById.GetChatById;

namespace GameHub.WebAPI.Endpoints.Chats;

public class GetChat : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chats/{chatId:guid}", async (
                    Guid chatId,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new Query(chatId), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(result.Error);
        })
                .WithName(nameof(GetChatById))
                .WithTags(nameof(Chat))
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .RequireAuthorization();
    }
}
