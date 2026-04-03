using GameHub.Application.Abstractions.Messaging;

namespace GameHub.Application.Features.Chats.Queries.GetUnreadMessagesCount;

public static partial class GetUnreadMessagesCountByChat
{
    public sealed record Query(Guid ChatId) : IQuery<int>;
}
