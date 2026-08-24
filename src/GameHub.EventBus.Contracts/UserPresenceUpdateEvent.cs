
namespace GameHub.EventBus.Contracts;

public sealed record class UserPresenceUpdateEvent(
    Guid UserId,
    DateTimeOffset LastActive
);