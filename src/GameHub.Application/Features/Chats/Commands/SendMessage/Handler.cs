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

namespace GameHub.Application.Features.Chats.Commands.SendMessage;

public static partial class ChatSendMessage
{
    public sealed class Handler : ICommandHandler<Command>
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

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var chat = await _context.Chats.FindAsync([request.ChatId], cancellationToken);

            var userId = _authenticatedUserService.UserId;

            if (chat is null)
            {
                return Result.Failure(ChatErrors.ChatGroupNotFound(request.ChatId));
            }

            var userProfile = await _context.UserProfiles.FindAsync([userId], cancellationToken);
            if (userProfile is null)
            {
                return Result.Failure(UserProfileErrors.NotFound(userId));
            }

            var isMember = await _context.ChatMembers.AnyAsync(cm => cm.ChatId == request.ChatId && cm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure(ChatErrors.NotParticipant(userId));
            }

            var createdAt = _dateTimeProvider.CurrentTimeUtc;

            var messageResult = chat.AddMessage(
                userId,
                request.Content,
                createdAt,
                _messagePreviewService);

            if (messageResult.IsFailure)
            {
                return Result.Failure(messageResult.Error);
            }

            var @event = new ChatMessageSentEvent
            {
                ChatId = chat.Id,
                UserId = userId,
                MessageId = messageResult.Value.Id
            };

            await _publishEndpoint.Publish(@event, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}