using GameHub.Domain.Abstractions;

namespace GameHub.Domain.Chats;

public sealed class ChatMessage : Entity<Guid>
{
    public const int MaxLength = 2000;
    public Guid ChatId { get; private init; } = default!;
    public string SenderUserId { get; private init; } = default!;
    public string Content { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    private ChatMessage() { }
    
    public ChatMessage(Guid chatId, string senderUserId, string content, DateTime createdAt)
    {
        ChatId = chatId;
        SenderUserId = senderUserId;
        Content = content;
        CreatedAt = createdAt;
    }
}
