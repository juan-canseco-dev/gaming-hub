using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Chats;

namespace GameHub.Application.Features.Chats.Queries.GetById;

public static partial class GetChatById
{
    public sealed record Query(Guid ChatId) : IQuery<ChatDto>;
}