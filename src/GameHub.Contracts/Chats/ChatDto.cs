namespace GameHub.Contracts.Chats;

public sealed record ChatDto(
    Guid Id,
    int ChannelId,
    string Slug,
    string Name,
    string Description,
    int ParticipantsCount,
    string? LastMesagePreview,
    DateTimeOffset? LastMessageAt,
    int UnreadCount
);
