using GameHub.Contracts.Profile;

namespace GameHub.Contracts.Chats;

public class MessageDto
{
    public Guid Id { get; set; }
    public UserDto? User { get; set; }
    public string? Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsSystem { get; set; }
}
