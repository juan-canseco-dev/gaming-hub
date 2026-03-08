using GameHub.Application.Abstractions.Messaging;
using GameHub.Application.Contracts.Chats;
using GameHub.Domain.Shared;

namespace GameHub.Application.Features.Chats.GetMessages;

public static partial class GetMessagesByChat
{
    public sealed record Query(
        Guid ChatId,
        int Limit = 50,
        string? Cursor = null
    ) : IQuery<CursorPage<MessageDto>>;
}
