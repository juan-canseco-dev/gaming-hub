
using GameHub.Application.Abstractions.Messaging;
using GameHub.Application.Contracts.Chats; 

namespace GameHub.Application.Features.Chats.GetMessage;

public static partial class GetMessageById
{
    public sealed record Query(Guid MessageId) : IQuery<MessageDto>;
}
