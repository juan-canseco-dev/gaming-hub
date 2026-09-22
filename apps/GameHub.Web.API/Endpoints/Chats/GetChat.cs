using Carter;
using GameHub.Application.Features.Chats.Queries.GetById;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using MediatR;
using static GameHub.Application.Features.Chats.Queries.GetById.GetChatById;
using GameHub.Contracts.Chats;

namespace GameHub.Web.API.Endpoints.Chats;

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
                : result.Error.ToProblem(StatusCodes.Status404NotFound);
        })
                .WithName(nameof(GetChatById))
                .WithTags(nameof(Chat))
                .WithSummary("Get a chat")
                .WithDescription("Returns one chat by its identifier for the authenticated user.")
                .Produces<ChatDto>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .RequireAuthorization();
    }
}
