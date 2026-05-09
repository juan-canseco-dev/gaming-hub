using FluentAssertions;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
namespace GameHub.Domain.UnitTests;

public sealed class ChatMemberTests
{
    [Fact]
    public void ReadUpTo_Should_UpdateLastReadAt_WhenTimestampIsGreaterThanCurrentLastReadAt()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 03, 24, 10, 00, 00, TimeSpan.Zero);
        var chatMember = new ChatMember(
            chatId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            createdAt: createdAt);

        var newTimestamp = createdAt.AddMinutes(10);

        // Act
        chatMember.ReadUpTo(newTimestamp);

        // Assert
        chatMember.LastReadAt.Should().Be(newTimestamp);
    }

    [Fact]
    public void ReadUpTo_Should_NotUpdateLastReadAt_WhenTimestampIsEqualToCurrentLastReadAt()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 03, 24, 10, 00, 00, TimeSpan.Zero);
        var chatMember = new ChatMember(
            chatId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            createdAt: createdAt);

        // Act
        chatMember.ReadUpTo(createdAt);

        // Assert
        chatMember.LastReadAt.Should().Be(createdAt);
    }

    [Fact]
    public void ReadUpTo_Should_NotUpdateLastReadAt_WhenTimestampIsLessThanCurrentLastReadAt()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 03, 24, 10, 00, 00, TimeSpan.Zero);
        var chatMember = new ChatMember(
            chatId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            createdAt: createdAt);

        var firstRead = createdAt.AddMinutes(15);
        chatMember.ReadUpTo(firstRead);

        var olderTimestamp = createdAt.AddMinutes(5);

        // Act
        chatMember.ReadUpTo(olderTimestamp);

        // Assert
        chatMember.LastReadAt.Should().Be(firstRead);
    }

    [Fact]
    public void UpdatePresence_Should_SetPresenceStatusToOnline_WhenLastConnectionIsWithin2Minutes()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 03, 24, 10, 00, 00, TimeSpan.Zero);
        var chatMember = new ChatMember(
            chatId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            createdAt: createdAt);
        var lastConnection = createdAt.AddMinutes(1);
        var currentTime = createdAt;
        // Act
        chatMember.UpdatePresence(lastConnection, currentTime);
        // Assert
        chatMember.PresenceStatus.Should().Be(PresenceStatus.Online);
    }

    [Fact]
    public void UpdatePresence_Should_SetPresenceStatusToAway_WhenLastConnectionIsMoreThan2MinutesAgo()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 03, 24, 10, 00, 00, TimeSpan.Zero);
        var chatMember = new ChatMember(
            chatId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            createdAt: createdAt);
        var lastConnection = createdAt.AddMinutes(3);
        var currentTime = createdAt;
        // Act
        chatMember.UpdatePresence(lastConnection, currentTime);
        // Assert
        chatMember.PresenceStatus.Should().Be(PresenceStatus.Away);
    }

    [Fact]
    public void UpdatePresence_Should_SetPresenceStatusToOffline_WhenLastConnectionIsMoreThan15MinutesAgo()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 03, 24, 10, 00, 00, TimeSpan.Zero);
        var chatMember = new ChatMember(
            chatId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            createdAt: createdAt);
        var lastConnection = createdAt.AddMinutes(16);
        var currentTime = createdAt;
        // Act
        chatMember.UpdatePresence(lastConnection, currentTime);
        // Assert
        chatMember.PresenceStatus.Should().Be(PresenceStatus.Offline);
    }

    [Fact]
    public void Constructor_Should_SetLastReadAt_ToNull()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 03, 24, 10, 00, 00, TimeSpan.Zero);

        // Act
        var chatMember = new ChatMember(
            chatId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            createdAt: createdAt);

        // Assert
        chatMember.LastReadAt.Should().BeNull();
    }
}
