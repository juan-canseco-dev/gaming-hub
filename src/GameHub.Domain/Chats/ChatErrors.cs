using GameHub.Abstractions.Primitives;

namespace GameHub.Domain.Chats;


public static class ChatErrors
{

    public static Error AlreadyParticipant(Guid userId) =>
        new("Chat.AlreadyParticipant",
            $"User '{userId}' is already a participant of this chat.");

    public static Error NotParticipant(Guid userId) =>
        new("Chat.NotParticipant",
            $"User '{userId}' is not a participant of this chat.");

    public static Error ChatGroupNotFound(Guid chatGroupId) =>
        new("Chat.ChatGroupNotFound",
            $"Chat group '{chatGroupId}' was not found.");
}