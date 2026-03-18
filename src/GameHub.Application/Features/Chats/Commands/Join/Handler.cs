using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Domain.Abstractions;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using GameHub.EventBus.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Features.Chats.Commands.Join;

public static partial class JoinChat
{
    public sealed class Handler : ICommandHandler<Command>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuthenticatedUserService _authenticatedUser;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IPublishEndpoint _publishEndpoint;

        public Handler(
            IApplicationDbContext context,
            IAuthenticatedUserService authenticatedUser,
            IDateTimeProvider dateTimeProvider, 
            IPublishEndpoint publishEndpoint
        )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _authenticatedUser = authenticatedUser ?? throw new ArgumentNullException(nameof(IAuthenticatedUserService));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var userId = _authenticatedUser.UserId;
            var chat = await _context.Chats.FindAsync([request.ChatId], cancellationToken);
            if (chat is null)
            {
                return Result.Failure(ChatErrors.ChatGroupNotFound(request.ChatId));
            }
            
            var userProfile = await _context.UserProfiles.FindAsync([userId], cancellationToken);
            if (userProfile is null)
            {
                return Result.Failure(UserProfileErrors.NotFound(userId));
            }
            
            var isMember = await _context
                .ChatMembers
                .AnyAsync(cm => cm.ChatId == request.ChatId && cm.UserId == userId, cancellationToken);


            if (isMember)
            {
                return Result.Failure(ChatErrors.AlreadyParticipant(userId));
            }

            var joinedAt = _dateTimeProvider.CurrentTimeUtc;

            var joinedUserMessage = chat.Join(userId, userProfile.Username, joinedAt);

            var @event = new ChatMemberJoinedEvent
            {
                ChatId = chat.Id,
                UserId = userId,
                MessageId = joinedUserMessage.Id
            };

            await _publishEndpoint.Publish(@event, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}


