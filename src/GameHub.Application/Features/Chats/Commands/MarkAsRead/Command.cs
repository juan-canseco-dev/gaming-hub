
using GameHub.Application.Abstractions.Messaging;

namespace GameHub.Application.Features.Chats.Commands.MarkAsRead;

public static partial class MarkChatAsRead
{
    public sealed record Command(
        Guid ChatId
    ) : ICommand;
}