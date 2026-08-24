using GameHub.Application.Abstractions.Messaging;

namespace GameHub.Application.Features.Presence.Queries.GetOnlineUsersCount;

public static partial class GetOnlineUsersCount
{
    public sealed record Query(Guid ChatId) : IQuery<int>;
}