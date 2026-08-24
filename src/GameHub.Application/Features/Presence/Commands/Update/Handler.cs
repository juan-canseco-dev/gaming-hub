using GameHub.Abstractions.Primitives;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Domain.Users;
using GameHub.EventBus.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Features.Presence.Commands.Update;

public static partial class UpdatePresence
{
    public sealed class Handler : ICommandHandler<Command>
    {
        private readonly IAuthenticatedUserService _authService;
        private readonly IApplicationDbContext _context;
        private readonly IDateTimeProvider _timeProvider;
        private readonly IPublishEndpoint _publisher;

        public Handler(
            IAuthenticatedUserService authService, 
            IApplicationDbContext context, 
            IDateTimeProvider timeProvider,
            IPublishEndpoint publisher
        )
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var currentUserId = _authService.UserId;
            var userPresence = await _context.UserPresences.FirstOrDefaultAsync(
                x => x.UserId == currentUserId,
                cancellationToken
            );

            if (userPresence is null)
            {
                return Result.Failure(UserProfileErrors.NotFound(currentUserId));
            }

            var wasUpdated = userPresence.Update(_timeProvider.CurrentTimeUtc);
            if (!wasUpdated)
            {
                return Result.Success();
            }

            var @event = new UserPresenceUpdateEvent(currentUserId, userPresence.LastActive);
            await _publisher.Publish(@event, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
