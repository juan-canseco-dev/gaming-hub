using GameHub.Application.Abstractions.Messaging;

namespace GameHub.Application.Features.Chats.SendMessage;

public static partial class ChatSendMessage
{
    public sealed record Command(
        Guid ChatId,
        Guid UserId,
        string Content) : ICommand;
}

