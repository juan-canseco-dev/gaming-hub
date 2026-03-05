using GameHub.Domain.Abstractions;

namespace GameHub.Domain.Chats;

public sealed class ChatMessage : Entity<Guid>
{
    public Guid ChatId { get; private init; } = default!;
    public Guid SenderUserId { get; private init; } = default!;
    public string Content { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    private ChatMessage() { }
    
    public ChatMessage(Guid chatId, Guid senderUserId, string content, DateTimeOffset createdAt)
    {
        ChatId = chatId;
        SenderUserId = senderUserId;
        Content = content;
        CreatedAt = createdAt;
    }
}
