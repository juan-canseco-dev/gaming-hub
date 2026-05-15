using GameHub.Domain.Abstractions;
namespace GameHub.Domain.Chats;

public sealed class ChatMember : Entity<Guid>
{
    public Guid ChatId { get; private init; } = default!;
    public Guid UserId { get; private init; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastReadAt { get; private set; }

    public  Chat Chat { get; private init; } = default!;

    public void ReadUpTo(DateTimeOffset timestamp)
    {
        if (timestamp > (LastReadAt ?? DateTimeOffset.MinValue))
        {
            LastReadAt = timestamp;
        }
    }

   

    public ChatMember(Guid chatId, Guid userId, DateTimeOffset createdAt)
    {
        ChatId = chatId;
        UserId = userId;
        CreatedAt = createdAt;
        LastReadAt = null;
    }

    private ChatMember() { }
}
