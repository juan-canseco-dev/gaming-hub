
using GameHub.Domain.Abstractions;
using GameHub.Domain.Users;

namespace GameHub.Domain.Presence;

public class UserPresence : Entity<Guid>
{
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan AwayThreshold = TimeSpan.FromMinutes(15);

    public Guid UserId { get; }
    public DateTimeOffset LastActive { get; private set; }
    public UserProfile UserProfile { get; } = default!;

    public bool Update(DateTimeOffset currentTime)
    {
        if (currentTime <= LastActive)
        {
            return false;
        }

        LastActive = currentTime;
        return true;
    }

    public PresenceStatus GetStatus(DateTimeOffset currentTime)
    {
        var elapsed = currentTime - LastActive;

        if (elapsed <= OnlineThreshold)
        {
            return PresenceStatus.Online;
        }

        return elapsed <= AwayThreshold
            ? PresenceStatus.Away
            : PresenceStatus.Offline;
    }

    public static DateTimeOffset GetOnlineCutoff(DateTimeOffset currentTime) =>
        currentTime.Subtract(OnlineThreshold);

    private UserPresence() { }

    public UserPresence(Guid userId, DateTimeOffset lastActive)
    {
        UserId = userId;
        LastActive = lastActive;
    }
}
