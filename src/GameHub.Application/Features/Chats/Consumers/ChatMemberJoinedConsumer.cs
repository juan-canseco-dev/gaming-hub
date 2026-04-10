using GameHub.EventBus.Contracts;
using MassTransit;
using MediatR;
using GameHub.Application.Features.Chats.Queries.GetParticipantsCount;
using GameHub.Application.Features.Chats.Queries.GetMessage;
using GameHub.Application.Abstractions.Realtime.Chats;
using GameHub.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace GameHub.Application.Features.Chats.Consumers;

public class ChatMemberJoinedConsumer : IConsumer<ChatMemberJoinedEvent>
{
    private readonly IMediator _mediator;
    private readonly IUserJoinedChatNotifier _notifier;
    private readonly ILogger<ChatMemberJoinedConsumer> _logger;

    public ChatMemberJoinedConsumer(
        IMediator mediator, 
        IUserJoinedChatNotifier notifier, 
        ILogger<ChatMemberJoinedConsumer> logger
    )
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<ChatMemberJoinedEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;

        _logger.LogInformation(
            "Consuming {EventName} for ChatId {ChatId} and MessageId {MessageId}.",
            nameof(ChatMemberJoinedEvent),
            message.ChatId,
            message.MessageId);

        var participantCountQuery = new GetParticipantCountByChat.Query(message.ChatId);
        var participantCountResult = await _mediator.Send(participantCountQuery, context.CancellationToken);

        if (participantCountResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to get participant count for ChatId {ChatId}. Error: {ErrorCode} - {ErrorMessage}",
                message.ChatId,
                participantCountResult.Error.Code,
                participantCountResult.Error.Description);

            return;
        }

        var getMessageQuery = new GetMessageById.Query(message.MessageId);
        var getMessageResult = await _mediator.Send(getMessageQuery, context.CancellationToken);

        if (getMessageResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to get message {MessageId} for ChatId {ChatId}. Error: {ErrorCode} - {ErrorMessage}",
                message.MessageId,
                message.ChatId,
                getMessageResult.Error.Code,
                getMessageResult.Error.Description);

            return;
        }

        var notification = new UserJoinedNotification(
            ChatId: message.ChatId,
            NumberOfParticipants: participantCountResult.Value,
            Message: getMessageResult.Value);

        _logger.LogInformation(
            "Sending user-joined-chat notification for ChatId {ChatId} with {ParticipantCount} participants.",
            message.ChatId,
            participantCountResult.Value);

        await _notifier.NotifyAsync(message.ChatId, notification, context.CancellationToken);

        _logger.LogInformation(
            "User-joined-chat notification sent successfully for ChatId {ChatId}.",
            message.ChatId);
    }
}
