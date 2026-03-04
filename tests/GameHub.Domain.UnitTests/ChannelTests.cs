
using GameHub.Domain.Chats;

namespace GameHub.Domain.UnitTests;

public sealed class ChannelTests
{
    public static IEnumerable<object[]> PredefinedChannels()
    {
        yield return new object[]
        {
            Channel.GeneralGaming,
            1,
            "General Gaming",
            "general-gaming",
            "Talk about what you're currently playing and gaming news."
        };

        yield return new object[]
        {
            Channel.RetroGaming,
            2,
            "Retro Gaming",
            "retro-gaming",
            "Classic consoles, arcade games and nostalgia."
        };

        yield return new object[]
        {
            Channel.RpgAndStory,
            3,
            "RPG & Story",
            "rpg-story",
            "Story-driven games, lore and character builds."
        };

        yield return new object[]
        {
            Channel.CompetitivePlay,
            4,
            "Competitive Play",
            "competitive",
            "Ranked strategies, esports and multiplayer."
        };

        yield return new object[]
        {
            Channel.IndieAndCreative,
            5,
            "Indie & Creative",
            "indie-creative",
            "Indie games, cozy games and creative discussions."
        };
    }

    [Theory]
    [MemberData(nameof(PredefinedChannels))]
    public void Predefined_channels_have_expected_values(
        Channel channel,
        int expectedId,
        string expectedName,
        string expectedSlug,
        string expectedDescription)
    {
        Assert.Equal(expectedId, channel.Id);
        Assert.Equal(expectedName, channel.Name);
        Assert.Equal(expectedSlug, channel.Slug);
        Assert.Equal(expectedDescription, channel.Description);
    }

    [Theory]
    [InlineData(1, nameof(Channel.GeneralGaming))]
    [InlineData(2, nameof(Channel.RetroGaming))]
    [InlineData(3, nameof(Channel.RpgAndStory))]
    [InlineData(4, nameof(Channel.CompetitivePlay))]
    [InlineData(5, nameof(Channel.IndieAndCreative))]
    public void FromIdResult_valid_id_returns_success_with_expected_instance(int id, string expectedStaticName)
    {
        var result = Channel.FromIdResult(id);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.NotNull(result.Value);

        // Strongest assertion: reference equality with the predefined static instance
        var expected = expectedStaticName switch
        {
            nameof(Channel.GeneralGaming) => Channel.GeneralGaming,
            nameof(Channel.RetroGaming) => Channel.RetroGaming,
            nameof(Channel.RpgAndStory) => Channel.RpgAndStory,
            nameof(Channel.CompetitivePlay) => Channel.CompetitivePlay,
            nameof(Channel.IndieAndCreative) => Channel.IndieAndCreative,
            _ => throw new System.InvalidOperationException("Unknown channel.")
        };

        Assert.Same(expected, result.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(999)]
    public void FromIdResult_invalid_id_returns_failure_with_expected_error(int id)
    {
        var result = Channel.FromIdResult(id);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);

        var expectedError = ChannelErrors.InvalidId(id);

        Assert.Equal(expectedError.Code, result.Error.Code);
        Assert.Equal(expectedError.Description, result.Error.Description);
    }
}