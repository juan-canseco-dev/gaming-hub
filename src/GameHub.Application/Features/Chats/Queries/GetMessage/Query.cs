
using GameHub.Application.Abstractions.Messaging;
using GameHub.Contracts.Chats;

namespace GameHub.Application.Features.Chats.Queries.GetMessage;

public static partial class GetMessageById
{
    public sealed record Query(Guid MessageId) : IQuery<MessageDto>;
}
