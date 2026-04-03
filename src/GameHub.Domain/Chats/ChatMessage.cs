using GameHub.Domain.Abstractions;

namespace GameHub.Domain.Chats;

public sealed class ChatMessage : Entity<Guid>
{
    public Guid ChatId { get; private init; } = default!;
    public Guid SenderUserId { get; private init; } = default!;
    public string Content { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public ChatMessageType Type { get; private set; } 
    private ChatMessage() { }
    
    public ChatMessage(
        Guid id,
        Guid chatId, 
        Guid senderUserId, 
        string content, 
        DateTimeOffset createdAt, 
        ChatMessageType type
    )
    {
        Id = id;
        ChatId = chatId;
        SenderUserId = senderUserId;
        Content = content;
        CreatedAt = createdAt;
        Type = type;
    }
}
