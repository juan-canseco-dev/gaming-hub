using FluentAssertions;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Data;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using GameHub.EventBus.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using GameHub.Application.Features.Chats.SendMessage;
using MockQueryable.Moq;


namespace GameHub.Application.UnitTests.Features.Chats.Send;

public sealed class SendMessageHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();

    private readonly MessagePreviewService _messagePreviewService = new();

    private readonly ChatSendMessage.Handler _handler;

    public SendMessageHandlerTests()
    {
        _handler = new ChatSendMessage.Handler(
            _contextMock.Object,
            _dateTimeProviderMock.Object,
            _publishEndpointMock.Object,
            _messagePreviewService);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenChatDoesNotExist()
    {
        // Arrange
        var command = new ChatSendMessage.Command(Guid.NewGuid(), Guid.NewGuid(), "hello");

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
            x => x.Publish(It.IsAny<ChatMessageSentEvent>(), It.IsAny<CancellationToken>()),
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
        var command = new ChatSendMessage.Command(chat.Id, Guid.NewGuid(), "hello");

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
            x => x.Publish(It.IsAny<ChatMessageSentEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _contextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserIsNotParticipant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chat = CreateChat();

        var userProfile = new UserProfile(
            userId,
            "player1@example.com",
            "player1",
            "Player One",
            DateTimeOffset.UtcNow);

        var command = new ChatSendMessage.Command(chat.Id, userId, "hello");

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
            new ChatMember(chat.Id, Guid.NewGuid(), DateTimeOffset.UtcNow)
        };
        var chatMembersDbSetMock = chatMembers.BuildMockDbSet();

        _contextMock.Setup(x => x.Chats).Returns(chatsDbSetMock.Object);
        _contextMock.Setup(x => x.UserProfiles).Returns(userProfilesDbSetMock.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembersDbSetMock.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ChatErrors.NotParticipant(userId));

        _publishEndpointMock.Verify(
            x => x.Publish(It.IsAny<ChatMessageSentEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _contextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenMessageContentIsInvalid()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        var chat = CreateChat();
        chat.Join(userId, "player1", now.AddMinutes(-5));

        var userProfile = new UserProfile(
            userId,
            "player1@example.com",
            "player1",
            "Player One",
            now.AddDays(-1));

        var command = new ChatSendMessage.Command(chat.Id, userId, string.Empty);

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
            new ChatMember(chat.Id, userId, now.AddMinutes(-10))
        };
        var chatMembersDbSetMock = chatMembers.BuildMockDbSet();

        _contextMock.Setup(x => x.Chats).Returns(chatsDbSetMock.Object);
        _contextMock.Setup(x => x.UserProfiles).Returns(userProfilesDbSetMock.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembersDbSetMock.Object);

        _dateTimeProviderMock
            .Setup(x => x.CurrentTimeUtc)
            .Returns(now);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MessageErrors.MessageContentRequired());

        _publishEndpointMock.Verify(
            x => x.Publish(It.IsAny<ChatMessageSentEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _contextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_AddMessage_AndPublishEvent_WhenCommandIsValid()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var content = "Hello everyone";

        var chat = CreateChat();
        chat.Join(userId, "player1", now.AddMinutes(-10));

        var userProfile = new UserProfile(
            userId,
            "player1@example.com",
            "player1",
            "Player One",
            now.AddDays(-1));

        var command = new ChatSendMessage.Command(chat.Id, userId, content);

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
            new ChatMember(chat.Id, userId, now.AddMinutes(-10))
        };
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

        ChatMessageSentEvent? publishedEvent = null;

        _publishEndpointMock
            .Setup(x => x.Publish(It.IsAny<ChatMessageSentEvent>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((evt, _) =>
                publishedEvent = (ChatMessageSentEvent)evt)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        chat.Messages.Should().ContainSingle(x =>
            x.SenderUserId == userId &&
            x.Content == content &&
            x.Type == ChatMessageType.User
        );

        var message = chat.Messages.Single(x =>
            x.SenderUserId == userId &&
            x.Content == content &&
            x.Type == ChatMessageType.User);

        chat.LastMesageId.Should().Be(message.Id);
        chat.LastMessageAt.Should().Be(now);
        chat.LastMessagePreview.Should().Be(content);

        publishedEvent.Should().NotBeNull();
        publishedEvent!.ChatId.Should().Be(chat.Id);
        publishedEvent.UserId.Should().Be(userId);
        publishedEvent.MessageId.Should().Be(message.Id);

        _publishEndpointMock.Verify(
            x => x.Publish(It.IsAny<ChatMessageSentEvent>(), It.IsAny<CancellationToken>()),
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