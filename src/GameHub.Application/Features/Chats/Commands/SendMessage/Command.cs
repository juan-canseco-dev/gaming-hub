using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Chats;

namespace GameHub.Application.Features.Chats.Commands.SendMessage;

public static partial class ChatSendMessage
{
    public sealed record Command(
        Guid ChatId,
        string Content
    ) : ICommand<MessageDto>;
}

