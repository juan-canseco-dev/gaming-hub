using GameHub.Application.Contracts.Profile;

namespace GameHub.Application.Contracts.Chats;

public class MessageDto
{
    public Guid Id { get; set; }
    public UserDto? User { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsSystem { get; set; }
}
