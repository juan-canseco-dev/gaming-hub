using GameHub.Application.Abstractions.Realtime.Presence;
using GameHub.Contracts.Notifications;
using GameHub.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GameHub.Infrastructure.Realtime;

internal sealed class SignalRUpdatePresenceNotifier : IUpdatePresenceNotifier
{
    private readonly IHubContext<ChatHub, ChatClientAdapter> _hubContext;
    private readonly ILogger<SignalRUpdatePresenceNotifier> _logger;

    public SignalRUpdatePresenceNotifier(
        IHubContext<ChatHub, ChatClientAdapter> hubContext, 
        ILogger<SignalRUpdatePresenceNotifier> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task NotifyAsync(
        Guid chatId,
        UserPresenceUpdatedNotification notification, 
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(notification);
        var groupName = ChatHub.GetChatGroupName(chatId);

        await _hubContext.Clients
           .Group(groupName)
           .OnUserPresenceUpdated(notification);

        _logger.LogDebug(
            "Sent SignalR presence notification to ChatId {ChatId} for UserId {UserId}",
            chatId,
            notification.Presence.UserId);
    }
}
