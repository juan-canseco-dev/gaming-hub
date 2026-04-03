using Carter;
using GameHub.Application.Features.Channels.GetList;
using GameHub.Domain.Chats;
using MediatR;
using static GameHub.Application.Features.Channels.GetList.GetChannels;

namespace GameHub.WebAPI.Endpoints.Channels;

public class GetList : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/channels", async (
         IMediator mediator,
         CancellationToken cancellationToken
        ) =>
        {
            var query = new Query();
            var result = await mediator.Send(query, cancellationToken);
            if (result.IsFailure) return Results.BadRequest(result.Error);
            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status200OK)
        .WithName(nameof(GetChannels))
        .WithTags(nameof(Channel));
    }
}
