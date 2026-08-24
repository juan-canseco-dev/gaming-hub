using GameHub.Contracts.Presence;

namespace GameHub.Contracts.Notifications;

public record UserPresenceUpdatedNotification(
    UserPresenceDto Presence,
    int OnlineUsersCount
);
