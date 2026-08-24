namespace GameHub.Contracts.Profile;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Fullname { get; set; } = null!;
    public GameHub.Contracts.Presence.UserPresenceDto? Presence { get; set; }
}
