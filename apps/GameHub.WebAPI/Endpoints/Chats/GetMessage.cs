using Carter;
using GameHub.Application.Features.Chats.Queries.GetMessage;
using GameHub.Domain.Chats;
using MediatR;

namespace GameHub.WebAPI.Endpoints.Chats;

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
            if (result.IsFailure) return Results.NotFound(result.Error);
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName(nameof(GetMessage))
        .WithTags(nameof(Chat));
    }

}
