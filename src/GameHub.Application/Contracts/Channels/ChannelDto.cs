
namespace GameHub.Application.Contracts.Channels;

public sealed record ChannelDto(
    int Id,
    Guid ChatId,
    string Slug,
    string Description,
    int ParticipantsCount
);