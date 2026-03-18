using Carter;
using GameHub.Domain.Chats;
using GameHub.Application.Features.Chats.Queries.GetParticipants;
using MediatR;

namespace GameHub.WebAPI.Endpoints.Chats;

public class GetParticipants : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chat/{chatId:guid}/members", async (
         IMediator mediator,
         CancellationToken cancellationToken,
         Guid chatId,
         [AsParameters]
         Request request
        ) =>
        {
            var query = new GetChatParticipants.Query(
                ChatId: chatId,
                Limit: request.Limit,
                Cursor: request.Cursor
            );

            var result = await mediator.Send(query, cancellationToken);
            if (result.IsFailure) return Results.BadRequest(result.Error);
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName(nameof(GetParticipants))
        .WithTags(nameof(Chat));
    }

    internal sealed record Request(int Limit, string? Cursor);
}
