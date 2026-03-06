namespace GameHub.EventBus.Contracts;

public class ChatMessageSentEvent
{
    public Guid UserId { get; init; }
    public Guid ChatId { get; init; }
    public Guid MessageId { get; init; }
}
