
using GameHub.Domain.Chats;

namespace GameHub.Domain.UnitTests;

public sealed class ChatTests
{
    [Fact]
    public void Create_valid_channelId_returns_success_and_initializes_state()
    {
        // Arrange
        var channelId = 1; // Channel.GeneralGaming exists
        var createdAt = new DateTime(2026, 03, 03, 10, 30, 00, DateTimeKind.Utc);
        var expectedCreatedAt = new DateTimeOffset(createdAt);

        // Act
        var result = Chat.Create(channelId, createdAt);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);

        var chat = result.Value;
        Assert.NotNull(chat);

        Assert.Equal(channelId, chat.ChannelId);
        Assert.Equal(expectedCreatedAt, chat.CreatedAt);

        Assert.Empty(chat.Members);
        Assert.Empty(chat.Messages);

        // New perf-related state
        Assert.Equal(default, chat.LastMessageAt);
        Assert.Equal(default, chat.LastMesageId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(999)]
    public void Create_invalid_channelId_returns_failure_and_propagates_channel_error(int invalidChannelId)
    {
        // Arrange
        var createdAt = new DateTime(2026, 03, 03, 10, 30, 00, DateTimeKind.Utc);
        var expectedError = ChannelErrors.InvalidId(invalidChannelId);

        // Act
        var result = Chat.Create(invalidChannelId, createdAt);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);

        Assert.Equal(expectedError.Code, result.Error.Code);
        Assert.Equal(expectedError.Description, result.Error.Description);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create_valid_ids_return_success(int validChannelId)
    {
        // Arrange
        var createdAt = DateTime.UtcNow;

        // Act
        var result = Chat.Create(validChannelId, createdAt);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(validChannelId, result.Value.ChannelId);
        Assert.Equal(new DateTimeOffset(createdAt), result.Value.CreatedAt);
    }
}