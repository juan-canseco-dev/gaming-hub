
using GameHub.Contracts.Chats;

namespace GameHub.Contracts.Notifications;

public sealed record UserJoinedNotification(
     Guid ChatId,
     int NumberOfParticipants,
     MessageDto Message
);