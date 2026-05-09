using GameHub.Domain.Abstractions;

namespace GameHub.Domain.Chats;

public sealed class ChatMember : Entity<Guid>
{
    public Guid ChatId { get; private init; } = default!;
    public Guid UserId { get; private init; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastReadAt { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public PresenceStatus? PresenceStatus { get; private set; }

    public  Chat Chat { get; private init; } = default!;

    public void ReadUpTo(DateTimeOffset timestamp)
    {
        if (timestamp > (LastReadAt ?? DateTimeOffset.MinValue))
        {
            LastReadAt = timestamp;
        }
    }

    public void UpdatePresence(DateTimeOffset lastConnection, DateTimeOffset currentTime)
    {
        if (lastConnection > (LastSeenAt ?? DateTimeOffset.MinValue))
        {
            LastSeenAt = lastConnection;
            var difference = lastConnection - currentTime;

            if (difference <= TimeSpan.FromMinutes(2))
            {
                PresenceStatus = PresenceStatus.Online;
                return;
            }

            if (difference <= TimeSpan.FromMinutes(15))
            {
                PresenceStatus = PresenceStatus.Away;
                return;
            }

            PresenceStatus = PresenceStatus.Offline;
        }
    }


    public ChatMember(Guid chatId, Guid userId, DateTimeOffset createdAt)
    {
        ChatId = chatId;
        UserId = userId;
        CreatedAt = createdAt;
        LastReadAt = null;
        LastSeenAt = null;
        PresenceStatus = null;
    }

    private ChatMember() { }
}
