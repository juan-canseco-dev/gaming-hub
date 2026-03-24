using GameHub.Contracts.Notifications;

namespace GameHub.Application.Abstractions.Realtime.Chats;

public interface IUserJoinedChatNotifier
{
    Task NotifyAsync(
        Guid chatId,
        UserJoinedNotification notification,
        CancellationToken cancellationToken = default);
}