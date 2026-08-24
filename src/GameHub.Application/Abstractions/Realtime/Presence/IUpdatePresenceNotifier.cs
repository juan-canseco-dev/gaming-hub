using GameHub.Contracts.Notifications;

namespace GameHub.Application.Abstractions.Realtime.Presence;

public interface IUpdatePresenceNotifier
{
    Task NotifyAsync(
        Guid chatId,
        UserPresenceUpdatedNotification notification, 
        CancellationToken cancellationToken = default
     );   
}
