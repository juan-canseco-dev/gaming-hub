namespace GameHub.EventBus.Contracts;

public class MessageSentEvent
{
    public Guid UserId { get; init; }
    public Guid ChatId { get; init; }
    public Guid MessageId { get; init; }
}
