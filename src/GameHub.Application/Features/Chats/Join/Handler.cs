using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Domain.Abstractions;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using GameHub.EventBus.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Features.Chats.Join;

public static partial class JoinChat
{
    public sealed class Handler : ICommandHandler<Command>
    {
        private readonly IApplicationDbContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IPublishEndpoint _publishEndpoint;

        public Handler(
            IApplicationDbContext context, 
            IDateTimeProvider dateTimeProvider, 
            IPublishEndpoint publishEndpoint
        )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var chat = await _context.Chats.FindAsync([request.ChatId], cancellationToken);
            if (chat is null)
            {
                return Result.Failure(ChatErrors.ChatGroupNotFound(request.ChatId));
            }
            
            var userProfile = await _context.UserProfiles.FindAsync([request.UserId], cancellationToken);
            if (userProfile is null)
            {
                return Result.Failure(UserProfileErrors.NotFound(request.UserId));
            }
            
            var isMember = await _context
                .ChatMembers
                .AnyAsync(cm => cm.ChatId == request.ChatId && cm.UserId == request.UserId, cancellationToken);


            if (isMember)
            {
                return Result.Failure(ChatErrors.AlreadyParticipant(request.UserId));
            }

            var joinedAt = _dateTimeProvider.CurrentTimeUtc;

            var joinedUserMessage = chat.Join(request.UserId, userProfile.Username, joinedAt);

            var @event = new ChatMemberJoinedEvent
            {
                ChatId = chat.Id,
                UserId = request.UserId,
                MessageId = joinedUserMessage.Id
            };

            await _publishEndpoint.Publish(@event, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}


