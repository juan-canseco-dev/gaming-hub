using Carter;
using GameHub.Application.Features.Chats.Queries.GetMessages;
using GameHub.Domain.Chats;
using MediatR;

namespace GameHub.WebAPI.Endpoints.Chats;

public class GetMessages : ICarterModule
{

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chat/{chatId:guid}/messages", async (
         IMediator mediator,
         CancellationToken cancellationToken,
         Guid chatId,
         [AsParameters] 
         Request request
        ) =>
        {
            var query = new GetMessagesByChat.Query(
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
        .WithName(nameof(GetMessages))
        .WithTags(nameof(Chat));
    }

    internal sealed record Request(int Limit, string? Cursor);
}
