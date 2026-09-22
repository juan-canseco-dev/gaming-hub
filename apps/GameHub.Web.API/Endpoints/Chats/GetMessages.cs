using Carter;
using GameHub.Application.Features.Chats.Queries.GetMessages;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using MediatR;
using GameHub.Abstractions.Pagination;
using GameHub.Contracts.Chats;

namespace GameHub.Web.API.Endpoints.Chats;

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
            if (result.IsFailure) return result.Error.ToProblem(StatusCodes.Status400BadRequest);
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .Produces<CursorPage<MessageDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("List chat messages")
        .WithDescription("Returns a newest-first cursor page of messages. Pass the returned next value as cursor to continue.")
        .WithName(nameof(GetMessages))
        .WithTags(nameof(Chat));
    }

    internal sealed record Request(int Limit, string? Cursor);
}
