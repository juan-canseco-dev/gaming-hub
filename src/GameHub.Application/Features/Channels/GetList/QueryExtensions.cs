using GameHub.Application.Abstractions.Messaging;
using GameHub.Application.Contracts.Channels;

namespace GameHub.Application.Features.Channels.GetList;

public static partial class GetChannels
{
    public sealed record Query() : IQuery<IReadOnlyCollection<ChannelDto>>;
}