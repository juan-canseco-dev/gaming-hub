using GameHub.Domain.Abstractions;
using GameHub.Domain.Chats.Errors;

namespace GameHub.Domain.Chats;

public sealed class Channel : Enumeration<Channel>
{
    public string Slug { get; }
    public string Description { get; }

    private Channel(int id, string name, string slug, string description)
        : base(id, name)
    {
        Slug = slug;
        Description = description;
    }

    // Predefined Channels

    public static readonly Channel GeneralGaming =
        new(1, "General Gaming", "general-gaming",
            "Talk about what you're currently playing and gaming news.");

    public static readonly Channel RetroGaming =
        new(2, "Retro Gaming", "retro-gaming",
            "Classic consoles, arcade games and nostalgia.");

    public static readonly Channel RpgAndStory =
        new(3, "RPG & Story", "rpg-story",
            "Story-driven games, lore and character builds.");

    public static readonly Channel CompetitivePlay =
        new(4, "Competitive Play", "competitive",
            "Ranked strategies, esports and multiplayer.");

    public static readonly Channel IndieAndCreative =
        new(5, "Indie & Creative", "indie-creative",
            "Indie games, cozy games and creative discussions.");

    public static Result<Channel> FromIdResult(int id)
    {
        var channel = FromValue(id);

        return channel is null
            ? Result.Failure<Channel>(ChannelErrors.InvalidId(id))
            : Result.Success(channel);
    }
}