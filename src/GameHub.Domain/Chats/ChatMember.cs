namespace GameHub.Domain.Chats;

public sealed class ChatMember
{
    public Guid ChatId { get; private init; } = default!;
    public Guid UserId { get; private init; } = default!;
    public DateTime CreatedAt { get; private set; }

    public ChatMember(Guid chatId, Guid userId, DateTime createdAt)
    {
        ChatId = chatId;
        UserId = userId;
        CreatedAt = createdAt;
    }
    private ChatMember() { }
}
