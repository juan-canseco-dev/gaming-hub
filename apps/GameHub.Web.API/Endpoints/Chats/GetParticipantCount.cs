using Carter;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using MediatR;
using GameHub.Application.Features.Chats.Queries.GetParticipantsCount;

namespace GameHub.Web.API.Endpoints.Chats;

public class GetParticipantCount : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chat/{chatId:guid}/members/count", async (
         IMediator mediator,
         CancellationToken cancellationToken,
         Guid chatId
        ) =>
        {
            var query = new GetParticipantCountByChat.Query(
                ChatId: chatId
            );

            var result = await mediator.Send(query, cancellationToken);
            if (result.IsFailure) return result.Error.ToProblem(StatusCodes.Status404NotFound);
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .Produces<int>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get the participant count")
        .WithDescription("Returns the number of members in a chat.")
        .WithName(nameof(GetParticipantCount))
        .WithTags(nameof(Chat));
    }
}
