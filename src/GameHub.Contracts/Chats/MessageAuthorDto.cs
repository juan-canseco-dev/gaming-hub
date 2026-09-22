using GameHub.Contracts.Presence;

namespace GameHub.Contracts.Chats;

public sealed class MessageAuthorDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Fullname { get; set; } = null!;
    public UserPresenceDto? Presence { get; set; }
}
