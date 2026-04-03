using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Chats;

namespace GameHub.Application.Features.Chats.Queries.GetMyChats;

public static partial class GetUserChats
{
    public sealed record Query() : IQuery<IReadOnlyCollection<ChatDto>>;
}
