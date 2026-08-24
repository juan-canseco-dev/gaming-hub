using GameHub.Application.Abstractions.Realtime.Chats;
using GameHub.Contracts.Notifications;
using GameHub.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;


namespace GameHub.Infrastructure.Realtime;

internal sealed class SignalRMessageSentNotifier : IMessageSentNotifier
{
    private readonly IHubContext<ChatHub, ChatClientAdapter> _hubContext;
    private readonly ILogger<SignalRMessageSentNotifier> _logger;

    public SignalRMessageSentNotifier(
      IHubContext<ChatHub, ChatClientAdapter> hubContext,
      ILogger<SignalRMessageSentNotifier> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task NotifyAsync(
        Guid chatId, 
        MessageNotification notification, 
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(notification);

        var groupName = ChatHub.GetChatGroupName(chatId);

        await _hubContext.Clients
            .Group(groupName)
            .MessageSent(notification);


        _logger.LogDebug(
            "Sent SignalR message notification to ChatId {ChatId} for MessageId {MessageId}",
            chatId,
            notification.Message.Id);
    }
}
