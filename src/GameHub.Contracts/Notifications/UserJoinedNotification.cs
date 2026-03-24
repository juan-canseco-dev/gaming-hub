
using GameHub.Contracts.Chats;

namespace GameHub.Contracts.Notifications;

public sealed record UserJoinedNotification(
     int NumberOfParticipants,
     MessageDto Message
);