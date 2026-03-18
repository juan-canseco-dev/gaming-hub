using GameHub.Application.Abstractions.Messaging;

namespace GameHub.Application.Features.Chats.Queries.GetPartcipantsCount;

public static partial class GetParticipantCountByChat
{
    public sealed record Query(Guid ChatId) : IQuery<int>;
}
