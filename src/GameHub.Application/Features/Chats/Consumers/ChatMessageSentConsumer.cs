using GameHub.Application.Abstractions.Realtime.Chats;
using GameHub.Application.Features.Chats.Queries.GetMessage;
using GameHub.EventBus.Contracts;
using GameHub.Contracts.Notifications;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GameHub.Application.Features.Chats.Consumers;

public class ChatMessageSentConsumer : IConsumer<ChatMessageSentEvent>
{
    private readonly IMediator _mediator;
    private readonly IMessageSentNotifier _notifier;
    private readonly ILogger<ChatMessageSentConsumer> _logger;

    public ChatMessageSentConsumer(
        IMediator mediator, 
        IMessageSentNotifier notifier, 
        ILogger<ChatMessageSentConsumer> logger
    )
    {
        _mediator = mediator ?? throw new ArgumentNullException( nameof( mediator ) );
        _notifier = notifier ?? throw new ArgumentNullException( nameof( notifier ) );
        _logger = logger ?? throw new ArgumentNullException( nameof( logger ) );
    }

    public async Task Consume(ConsumeContext<ChatMessageSentEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;

        _logger.LogInformation(
            "Consuming {EventName} for ChatId {ChatId} and MessageId {MessageId}.",
            nameof(ChatMemberJoinedEvent),
            message.ChatId,
            message.MessageId);

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

        var notification = new MessageNotification(
            ChatId: message.ChatId,
            Message: getMessageResult.Value
        );

        _logger.LogInformation(
            "Sending chat-message notification for ChatId {ChatId} and MessageId {MessageId}.",
            message.ChatId,
            message.MessageId);

        await _notifier.NotifyAsync(message.ChatId, notification, context.CancellationToken);

        _logger.LogInformation(
            "Chat-message notification sent successfully for ChatId {ChatId} and MessageId {MessageId}.",
            message.ChatId,
            message.MessageId);
    }
}
