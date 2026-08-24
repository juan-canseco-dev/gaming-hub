 using GameHub.Abstractions.Primitives;

namespace GameHub.Domain.Presence;

public sealed class PresenceStatus : Enumeration<PresenceStatus>
{
    public PresenceStatus(int id, string name) : base(id, name) { }
    public static readonly PresenceStatus Online = new(1, "Online");
    public static readonly PresenceStatus Away = new(2, "Away");
    public static readonly PresenceStatus Offline = new(3, "Offline"); 
}
