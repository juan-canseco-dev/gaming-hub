using GameHub.Domain.Abstractions;
using GameHub.Domain.Channels;
using GameHub.Domain.Users;
using GameHub.Abstractions.Primitives;

namespace GameHub.Domain.Chats;

public class Chat : Entity<Guid>
{
    public const int MaxMessageLength = 2000;
    public const int MaxPreviewLength = 200;

    private readonly List<ChatMember> _members = new();
    private readonly List<ChatMessage> _messages = new();
    public int ChannelId { get; private init; } = default!;
    public DateTimeOffset CreatedAt { get; private init; } = default!;
    public DateTimeOffset LastMessageAt { get; private set; }
    public string? LastMessagePreview { get; private set; }
    public Guid LastMessageId { get; private set; }
    public IReadOnlyCollection<ChatMember> Members => _members.AsReadOnly();
    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();
    public Channel Channel { get; private init; } = default!;

    private Chat() { }

    private Chat(int channelId, DateTimeOffset createdAt)
    {
        ChannelId = channelId;
        CreatedAt = createdAt;
    }

    public Result<ChatMessage> AddMessage(
        Guid senderUserId,
        string content,
        DateTimeOffset createdAt,
        MessagePreviewService service
    )
    {

        if (string.IsNullOrEmpty(content))
        {
            return Result.Failure<ChatMessage>(MessageErrors.MessageContentRequired());
        }

        if (content.Length > MaxMessageLength)
        {
            return Result.Failure<ChatMessage>(MessageErrors.MessageTooLong(MaxMessageLength));
        }

        var messageId = Guid.CreateVersion7();
        var message = new ChatMessage(messageId, this.Id, senderUserId, content, createdAt, ChatMessageType.User);

        _messages.Add(message);
        LastMessageAt = createdAt;
        LastMessageId = message.Id;
        LastMessagePreview = service.CreatePreview(content, MaxPreviewLength);

        return Result.Success(message);
    }

    public ChatMessage Join(
        Guid userId,
        string username,
        DateTimeOffset joinedAt
    )
    {
        var member = new ChatMember(Id, userId, joinedAt);
        _members.Add(member);

        var content = $"User {username} joined the chat.";

        var messageId = Guid.CreateVersion7();
        var message = new ChatMessage(
            messageId,
            Id,
            SystemUsers.AdminUserId,
            content,
            joinedAt,
            ChatMessageType.System
        );

        _messages.Add(message);
        LastMessageAt = joinedAt;
        LastMessageId = message.Id;
        LastMessagePreview = content;

        return message;
    }

    public static Result<Chat> Create(int channelId, DateTimeOffset createdAt)
    {
        var channel = Channel.FromIdResult(channelId);
        if (channel.IsFailure)
            return Result.Failure<Chat>(channel.Error);

        return Result.Success(new Chat(channelId, createdAt));
    }
}
