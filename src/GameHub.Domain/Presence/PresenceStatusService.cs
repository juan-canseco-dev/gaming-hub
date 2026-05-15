namespace GameHub.Domain.Presence;

public class PresenceStatusService
{
    public PresenceStatus GetStatus(DateTimeOffset lastActive, DateTimeOffset currentTime)
    {
        var timeDifference = currentTime - lastActive;
        if (timeDifference <= TimeSpan.FromMinutes(2))
        {
            return PresenceStatus.Online;
        }

        if (timeDifference <= TimeSpan.FromMinutes(15))
        {
            return PresenceStatus.Away;
        }

        return PresenceStatus.Offline;
    }
}
