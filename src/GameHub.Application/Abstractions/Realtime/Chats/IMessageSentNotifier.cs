

using GameHub.Contracts.Notifications;

namespace GameHub.Application.Abstractions.Realtime.Chats;

public interface IMessageSentNotifier
{
    Task NotifyAsync(
        Guid chatId,
        MessageNotification notification,
        CancellationToken cancellationToken = default
     );
}