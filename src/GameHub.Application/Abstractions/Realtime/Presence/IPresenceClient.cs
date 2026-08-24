using GameHub.Contracts.Notifications;

namespace GameHub.Application.Abstractions.Realtime.Presence;

public interface IPresenceClient
{
    Task OnUserPresenceUpdated(UserPresenceUpdatedNotification notification);
}
