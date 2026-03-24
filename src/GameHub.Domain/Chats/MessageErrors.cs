using GameHub.Abstractions.Primitives;

namespace GameHub.Domain.Chats;

public static class MessageErrors
{
    public static Error MessageContentRequired() =>
        new("Chat.MessageContentRequired",
            "Message content must be provided.");

    public static Error MessageTooLong(int maxLength) =>
        new("Chat.MessageTooLong",
            $"Message content must not exceed {maxLength} characters.");

    public static Error NotFound(Guid messageId) => new(
        "Messages.NotFound",
        $"The specified Message with the Id: {messageId} was not Found."
    );
}