
using GameHub.Contracts.Chats;

namespace GameHub.Contracts.Notifications;

public sealed record MessageNotification(
    Guid ChatId,
    MessageDto Message
);

