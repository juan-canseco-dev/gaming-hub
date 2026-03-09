using GameHub.Domain.Abstractions;

namespace GameHub.Application.Features.Chats.GetParticipants;

public static partial class GetChatParticipants
{
    internal static class Errors
    {
        public static Error InvalidCursor =>
        new(
            "ChatParticipants.InvalidCursor",
            "The provided cursor is invalid.");
    }
}
