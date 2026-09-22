namespace GameHub.Contracts.Chats;

public class MessageDto
{
    public Guid Id { get; set; }
    public MessageAuthorDto? User { get; set; }
    public string? Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsSystem { get; set; }
}
