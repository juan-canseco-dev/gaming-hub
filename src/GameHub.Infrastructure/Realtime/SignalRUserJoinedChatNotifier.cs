
using GameHub.Application.Abstractions.Realtime.Chats;
using GameHub.Contracts.Notifications;
using GameHub.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace GameHub.Infrastructure.Realtime;

internal sealed class SignalRUserJoinedChatNotifier : IUserJoinedChatNotifier
{
    private readonly IHubContext<ChatHub, ChatClientAdapter> _hubContext;
    private readonly ILogger<SignalRUserJoinedChatNotifier> _logger;

    public SignalRUserJoinedChatNotifier(
        IHubContext<ChatHub, ChatClientAdapter> hubContext,
        ILogger<SignalRUserJoinedChatNotifier> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task NotifyAsync(
        Guid chatId,
        UserJoinedNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var groupName = ChatHub.GetChatGroupName(chatId);

        await _hubContext.Clients
            .Group(groupName)
            .UserJoinedChat(notification);

        _logger.LogDebug(
            "Sent SignalR user-joined notification to ChatId {ChatId} with {ParticipantCount} participants for MessageId {MessageId}",
            chatId,
            notification.NumberOfParticipants,
            notification.Message.Id);
    }
}
