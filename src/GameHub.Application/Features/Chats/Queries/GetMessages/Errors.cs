using GameHub.Domain.Abstractions;


namespace GameHub.Application.Features.Chats.Queries.GetMessages;

public static partial class GetMessagesByChat
{
   internal static class Errors
    {
        public static Error InvalidCursor =>
        new(
            "ChatMessages.InvalidCursor",
            "The provided cursor is invalid.");
    }
}
