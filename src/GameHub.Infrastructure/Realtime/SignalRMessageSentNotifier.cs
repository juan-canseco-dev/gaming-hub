using GameHub.Application.Abstractions.Realtime.Chats;
using GameHub.Contracts.Notifications;
using GameHub.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;


namespace GameHub.Infrastructure.Realtime;

internal sealed class SignalRMessageSentNotifier : IMessageSentNotifier
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;
    private readonly ILogger<SignalRMessageSentNotifier> _logger;

    public SignalRMessageSentNotifier(
      IHubContext<ChatHub, IChatClient> hubContext,
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

        _logger.LogInformation(
            "Sending SignalR message notification to chat {ChatId}. MessageId: {MessageId}",
            chatId,
            notification.Message.Id);

        await _hubContext.Clients
            .Group(groupName)
            .MessageSent(notification);


        _logger.LogInformation(
            "SignalR message notification sent to chat {ChatId}.",
            chatId);
    }
}
