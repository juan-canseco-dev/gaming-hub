using GameHub.Application.Abstractions.Messaging;

namespace GameHub.Application.Features.Chats.Queries.GetTotalUnreadMessagesCount;

public static partial class GetTotalChatUnreadMessagesCount
{
    public sealed record Query() : IQuery<int>;
}
