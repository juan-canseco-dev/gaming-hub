using GameHub.Domain.Abstractions;

namespace GameHub.Domain.Chats;

public class Chat : Entity<Guid>
{
    private readonly List<ChatMember> _members = new();
    private readonly List<ChatMessage> _messages = new();
    public int ChannelId { get; private init; } = default!;
    public DateTime CreatedAt { get; private init; } = default!;
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyCollection<ChatMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();
    public Channel Channel { get; private init; } = default!;
    private Chat() { }

    private Chat(int channelId, DateTime createdAt)
    {
        ChannelId = channelId;
        CreatedAt = createdAt;
    }

    public static Result<Chat> Create(int channelId, DateTime createdAt)
    {
        var channel = Channel.FromIdResult(channelId);
        if (channel.IsFailure)
            return Result.Failure<Chat>(channel.Error);

        return Result.Success(new Chat(channelId, createdAt));
    }
}
