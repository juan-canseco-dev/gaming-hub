
namespace GameHub.Contracts.Presence;

public record UserPresenceDto(
    Guid UserId,
    DateTimeOffset LastActive,
    string Status
);