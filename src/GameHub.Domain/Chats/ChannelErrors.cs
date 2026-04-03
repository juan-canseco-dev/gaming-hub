using GameHub.Domain.Abstractions;
using GameHub.Abstractions.Primitives;

namespace GameHub.Domain.Chats;

public static class ChannelErrors
{
    public static Error InvalidId(int id) =>
        new("Channel.InvalidId", $"Invalid channel id: {id}.");
}