using GameHub.Domain.Abstractions;

namespace GameHub.Domain.Chats;


public static class ChatErrors
{


    public static Error UserIdRequired() =>
        new("Chat.UserIdRequired", "UserId must be provided.");

    public static Error AlreadyParticipant(Guid userId) =>
        new("Chat.AlreadyParticipant",
            $"User '{userId}' is already a participant of this chat.");

    public static Error NotParticipant(string userId) =>
        new("Chat.NotParticipant",
            $"User '{userId}' is not a participant of this chat.");

    public static Error ParticipantNotFound(string userId) =>
        new("Chat.ParticipantNotFound",
            $"Participant '{userId}' was not found in this chat.");

    public static Error InvalidChannelId(int channelId) =>
        new("Chat.InvalidChannelId",
            $"Channel with id '{channelId}' does not exist.");

    public static Error ChatGroupNotFound(Guid chatGroupId) =>
        new("Chat.ChatGroupNotFound",
            $"Chat group '{chatGroupId}' was not found.");
}