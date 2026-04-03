using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Chats;
using GameHub.Abstractions.Pagination;

namespace GameHub.Application.Features.Chats.Queries.GetMessages;

public static partial class GetMessagesByChat
{
    public sealed record Query(
        Guid ChatId,
        int Limit = 50,
        string? Cursor = null
    ) : IQuery<CursorPage<MessageDto>>;
}
