using FluentAssertions;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Data;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using GameHub.EventBus.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using GameHub.Application.Features.Chats.Join;
using MockQueryable.Moq;
using System.Linq.Expressions;

namespace GameHub.Application.UnitTests.Features.Chats.Join;

public sealed class JoinChatHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();

    private readonly JoinChat.Handler _handler;

    public JoinChatHandlerTests()
    {
        _handler = new JoinChat.Handler(
            _contextMock.Object,
            _dateTimeProviderMock.Object,
            _publishEndpointMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenChatDoesNotExist()
    {
        // Arrange
        var command = new JoinChat.Command(Guid.NewGuid(), Guid.NewGuid());

        var chatsDbSetMock = new Mock<DbSet<Chat>>();
        chatsDbSetMock
            .Setup(x => x.FindAsync(
                It.Is<object[]>(ids => (Guid)ids[0] == command.ChatId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Chat?)null);

        _contextMock.Setup(x => x.Chats).Returns(chatsDbSetMock.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ChatErrors.ChatGroupNotFound(command.ChatId));

        _publishEndpointMock.Verify(
            x => x.Publish(It.IsAny<ChatMemberJoinedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _contextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserProfileDoesNotExist()
    {
        // Arrange
        var chat = CreateChat();
        var command = new JoinChat.Command(chat.Id, Guid.NewGuid());

        var chatsDbSetMock = new Mock<DbSet<Chat>>();
        chatsDbSetMock
            .Setup(x => x.FindAsync(
                It.Is<object[]>(ids => (Guid)ids[0] == command.ChatId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chat);

        var userProfilesDbSetMock = new Mock<DbSet<UserProfile>>();
        userProfilesDbSetMock
            .Setup(x => x.FindAsync(
                It.Is<object[]>(ids => (Guid)ids[0] == command.UserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        _contextMock.Setup(x => x.Chats).Returns(chatsDbSetMock.Object);
        _contextMock.Setup(x => x.UserProfiles).Returns(userProfilesDbSetMock.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.NotFound(command.UserId));

        _publishEndpointMock.Verify(
            x => x.Publish(It.IsAny<ChatMemberJoinedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _contextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserAlreadyMember()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var chat = CreateChat();

        var userProfile = new UserProfile(
            userId,
            "existing@example.com",
            "existing-user",
            "Existing User",
            now);

        var command = new JoinChat.Command(chat.Id, userId);

        var chatsDbSetMock = new Mock<DbSet<Chat>>();
        chatsDbSetMock
            .Setup(x => x.FindAsync(
                It.Is<object[]>(ids => (Guid)ids[0] == command.ChatId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chat);

        var userProfilesDbSetMock = new Mock<DbSet<UserProfile>>();
        userProfilesDbSetMock
            .Setup(x => x.FindAsync(
                It.Is<object[]>(ids => (Guid)ids[0] == command.UserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userProfile);

        var chatMembers = new List<ChatMember>
        {
            new ChatMember(chat.Id, userId, now)
        };

        var chatMembersDbSetMock = chatMembers.BuildMockDbSet();

        _contextMock.Setup(x => x.Chats).Returns(chatsDbSetMock.Object);
        _contextMock.Setup(x => x.UserProfiles).Returns(userProfilesDbSetMock.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembersDbSetMock.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ChatErrors.AlreadyParticipant(userId));

        _publishEndpointMock.Verify(
            x => x.Publish(It.IsAny<ChatMemberJoinedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _contextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_JoinChat_AndPublishEvent_WhenCommandIsValid()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var chat = CreateChat();

        var userProfile = new UserProfile(
            userId,
            "player1@example.com",
            "player1",
            "Player One",
            now.AddDays(-1));

        var command = new JoinChat.Command(chat.Id, userId);

        var chatsDbSetMock = new Mock<DbSet<Chat>>();
        chatsDbSetMock
            .Setup(x => x.FindAsync(
                It.Is<object[]>(ids => (Guid)ids[0] == command.ChatId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chat);

        var userProfilesDbSetMock = new Mock<DbSet<UserProfile>>();
        userProfilesDbSetMock
            .Setup(x => x.FindAsync(
                It.Is<object[]>(ids => (Guid)ids[0] == command.UserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userProfile);

        var chatMembers = new List<ChatMember>();
        var chatMembersDbSetMock = chatMembers.BuildMockDbSet();

        _contextMock.Setup(x => x.Chats).Returns(chatsDbSetMock.Object);
        _contextMock.Setup(x => x.UserProfiles).Returns(userProfilesDbSetMock.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembersDbSetMock.Object);

        _contextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _dateTimeProviderMock
            .Setup(x => x.CurrentTimeUtc)
            .Returns(now);

        ChatMemberJoinedEvent? publishedEvent = null;

        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<ChatMemberJoinedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((evt, _) =>
                publishedEvent = (ChatMemberJoinedEvent)evt)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        chat.Members.Should().ContainSingle(x => x.UserId == userId);

        var message = chat.Messages.Should().ContainSingle(x =>
            x.Content == $"User {userProfile.Username} joined the chat.")
            .Subject;

        publishedEvent.Should().NotBeNull();
        publishedEvent!.ChatId.Should().Be(chat.Id);
        publishedEvent.UserId.Should().Be(userId);
        publishedEvent.MessageId.Should().Be(message.Id);

        _publishEndpointMock.Verify(
            x => x.Publish(It.IsAny<ChatMemberJoinedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _contextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Chat CreateChat()
    {
        var result = Chat.Create(
            channelId: 1,
            createdAt: DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();

        return result.Value;
    }
}