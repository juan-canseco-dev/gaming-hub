using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Profile;
using GameHub.Abstractions.Pagination;

namespace GameHub.Application.Features.Chats.Queries.GetParticipants;

public static partial class GetChatParticipants
{
    public sealed record Query(
        Guid ChatId, 
        int Limit, 
        string? Cursor
    ) : IQuery<CursorPage<UserDto>>;
}