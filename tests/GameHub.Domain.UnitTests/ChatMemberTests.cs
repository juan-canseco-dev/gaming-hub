using FluentAssertions;
using GameHub.Domain.Chats;
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
