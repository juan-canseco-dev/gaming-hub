using GameHub.Domain.Abstractions;

namespace GameHub.Domain.Users;

public class UserChat : Entity<Guid>
{
    public Guid ChatId { get; private init; }
    public Guid UserId { get; private init; }
    public DateTimeOffset CreatedAt { get; private set; }
    public UserChat(Guid chatId, Guid userId, DateTimeOffset createdAt)
    {
        ChatId = chatId;
        UserId = userId;
        CreatedAt = createdAt;
    }

    private UserChat() { }
}
