
using GameHub.Domain.Abstractions;
using GameHub.Domain.Users;

namespace GameHub.Domain.Presence;

public class UserPresence : Entity<Guid>
{
    public Guid UserId { get; }
    public DateTimeOffset LastActive { get; private set; }
    public UserProfile UserProfile{ get; } = default!;

    public void Update(DateTimeOffset currentTime)
    {
        if (currentTime > LastActive) 
        {
            LastActive = currentTime;
        }
    }

    private UserPresence() { }
    
    public UserPresence(Guid userId, DateTimeOffset lastActive)
    {
        UserId = userId;
        LastActive = lastActive;
    }
}