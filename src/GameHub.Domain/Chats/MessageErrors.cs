using GameHub.Domain.Abstractions;

namespace GameHub.Domain.Chats;

public static class MessageErrors
{
    public static Error MessageContentRequired() =>
        new("Chat.MessageContentRequired",
            "Message content must be provided.");

    public static Error MessageTooLong(int maxLength) =>
        new("Chat.MessageTooLong",
            $"Message content must not exceed {maxLength} characters.");

}