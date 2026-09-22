using Carter;
using GameHub.Application.Features.Chats.Queries.GetMessage;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using MediatR;
using GameHub.Contracts.Chats;

namespace GameHub.Web.API.Endpoints.Chats;

public class GetMessage : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chat/messages/{messageId:guid}", async (
         IMediator mediator,
         CancellationToken cancellationToken,
         Guid messageId
        ) =>
        {
            var query = new GetMessageById.Query(
                MessageId: messageId    
            );

            var result = await mediator.Send(query, cancellationToken);
            if (result.IsFailure) return result.Error.ToProblem(StatusCodes.Status404NotFound);
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .Produces<MessageDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a message")
        .WithDescription("Returns one chat message by its identifier.")
        .WithName(nameof(GetMessage))
        .WithTags(nameof(Chat));
    }

}
