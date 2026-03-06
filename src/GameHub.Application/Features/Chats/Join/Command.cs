using GameHub.Application.Abstractions.Messaging;

namespace GameHub.Application.Features.Chats.Join;

public static partial class JoinChat
{
    public sealed record Command(Guid ChatId, Guid UserId)  : ICommand;
}