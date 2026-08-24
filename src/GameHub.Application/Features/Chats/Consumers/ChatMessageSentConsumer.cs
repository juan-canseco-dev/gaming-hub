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

        _logger.LogDebug(
            "Consuming {EventName} for ChatId {ChatId} and MessageId {MessageId}.",
            nameof(ChatMessageSentEvent),
            message.ChatId,
            message.MessageId);

        var getMessageQuery = new GetMessageById.Query(message.MessageId);
        var getMessageResult = await _mediator.Send(getMessageQuery, context.CancellationToken);

        if (getMessageResult.IsFailure)
        {
            _logger.LogWarning(
                "Unable to process {EventName}: message {MessageId} for ChatId {ChatId} returned {ErrorCode}",
                nameof(ChatMessageSentEvent),
                message.MessageId,
                message.ChatId,
                getMessageResult.Error.Code);

            return;
        }

        var notification = new MessageNotification(
            ChatId: message.ChatId,
            Message: getMessageResult.Value
        );

        await _notifier.NotifyAsync(message.ChatId, notification, context.CancellationToken);

        _logger.LogDebug(
            "Processed {EventName} for ChatId {ChatId} and MessageId {MessageId}",
            nameof(ChatMessageSentEvent),
            message.ChatId,
            message.MessageId);
    }
}
