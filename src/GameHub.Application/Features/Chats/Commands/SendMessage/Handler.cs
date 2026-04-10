using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Messaging;
using GameHub.Abstractions.Primitives;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using GameHub.Domain.Users;
using GameHub.EventBus.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using GameHub.Contracts.Chats;
using GameHub.Contracts.Profile;

namespace GameHub.Application.Features.Chats.Commands.SendMessage;

public static partial class ChatSendMessage
{
    public sealed class Handler : ICommandHandler<Command,MessageDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IAuthenticatedUserService _authenticatedUserService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly MessagePreviewService _messagePreviewService;

        public Handler(
            IApplicationDbContext context,
            IAuthenticatedUserService authenticatedUserService,
            IDateTimeProvider dateTimeProvider,
            IPublishEndpoint publishEndpoint,
            MessagePreviewService messagePreviewService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _authenticatedUserService = authenticatedUserService ?? throw new ArgumentNullException(nameof(authenticatedUserService));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _messagePreviewService = messagePreviewService ?? throw new ArgumentNullException(nameof(messagePreviewService));
        }

        public async Task<Result<MessageDto>> Handle(Command request, CancellationToken cancellationToken)
        {
            var chat = await _context.Chats.FindAsync([request.ChatId], cancellationToken);

            var userId = _authenticatedUserService.UserId;

            if (chat is null)
            {
                return Result.Failure<MessageDto>(ChatErrors.ChatGroupNotFound(request.ChatId));
            }

            var userProfile = await _context.UserProfiles.FindAsync([userId], cancellationToken);
            if (userProfile is null)
            {
                return Result.Failure<MessageDto>(UserProfileErrors.NotFound(userId));
            }

            var isMember = await _context.ChatMembers.AnyAsync(cm => cm.ChatId == request.ChatId && cm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<MessageDto>(ChatErrors.NotParticipant(userId));
            }

            var createdAt = _dateTimeProvider.CurrentTimeUtc;

            var messageResult = chat.AddMessage(
                userId,
                request.Content,
                createdAt,
                _messagePreviewService);

            if (messageResult.IsFailure)
            {
                return Result.Failure<MessageDto>(messageResult.Error);
            }

            var @event = new ChatMessageSentEvent
            {
                ChatId = chat.Id,
                UserId = userId,
                MessageId = messageResult.Value.Id
            };

            await _publishEndpoint.Publish(@event, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return new MessageDto
            {
                Id = messageResult.Value.Id,
                User = new UserDto
                {
                    Id = userProfile.Id,
                    Email = userProfile.Email,
                    Fullname = userProfile.Fullname,
                    Username = userProfile.Username,
                },
                Content = messageResult.Value.Content,
                CreatedAt = createdAt,
                IsSystem = messageResult.Value.Type == ChatMessageType.System
            };
        }
    }
}