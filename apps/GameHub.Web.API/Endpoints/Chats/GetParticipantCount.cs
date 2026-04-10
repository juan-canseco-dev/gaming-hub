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
            if (result.IsFailure) return Results.NotFound(result.Error);
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName(nameof(GetParticipantCount))
        .WithTags(nameof(Chat));
    }
}
