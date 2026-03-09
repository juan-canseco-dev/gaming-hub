using GameHub.Application.Abstractions.Messaging;
using GameHub.Application.Contracts.Profile;
using GameHub.Domain.Shared;

namespace GameHub.Application.Features.Chats.GetParticipants;

public static partial class GetChatParticipants
{
    public sealed record Query(
        Guid ChatId, 
        int Limit, 
        string? Cursor
    ) : IQuery<CursorPage<UserDto>>;
}