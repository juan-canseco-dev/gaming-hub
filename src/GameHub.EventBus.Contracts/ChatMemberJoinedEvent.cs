namespace GameHub.EventBus.Contracts;

public class ChatMemberJoinedEvent
{
    public Guid ChatId { get; init; }
    public Guid UserId { get; init; }
    public Guid MessageId { get; init; }
}
