using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Features.Chats.Commands.MarkAsRead;
using GameHub.Domain.Chats;
using ChatFactory = GameHub.Application.UnitTests.Shared.Factories.ChatTestFactory;
using Moq;
using MockQueryable.Moq;
using FluentAssertions;


namespace GameHub.Application.UnitTests.Features.Chats.Commands.MarkAsRead;

public class MarkChatAsReadHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authServiceMock;
    private readonly Mock<IDateTimeProvider> _timeProviderMock;


    public MarkChatAsReadHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _authServiceMock = new Mock<IAuthenticatedUserService>();
        _timeProviderMock = new Mock<IDateTimeProvider>();
    }
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenChatDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var chats = new List<Chat>().BuildMockDbSet();
        var members = new List<ChatMember>().BuildMockDbSet();

        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);
        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _timeProviderMock.Setup(x => x.CurrentTimeUtc).Returns(now);

        var handler = new MarkChatAsRead.Handler(
            _contextMock.Object,
            _authServiceMock.Object,
            _timeProviderMock.Object);

        var command = new MarkChatAsRead.Command(chatId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ChatErrors.ChatGroupNotFound(chatId));

        _contextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotParticipant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var chat = ChatFactory.CreateNew(chatId, Channel.GeneralGaming.Id, now);

        var chats = new List<Chat> { chat }.BuildMockDbSet();
        var members = new List<ChatMember>().BuildMockDbSet();

        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);
        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _timeProviderMock.Setup(x => x.CurrentTimeUtc).Returns(now);

        var handler = new MarkChatAsRead.Handler(
            _contextMock.Object,
            _authServiceMock.Object,
            _timeProviderMock.Object);

        var command = new MarkChatAsRead.Command(chatId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ChatErrors.NotParticipant(userId));

        _contextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldMarkChatAsRead_AndSaveChanges_WhenMembershipExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2);
        var readUpTo = DateTimeOffset.UtcNow;

        var chat = ChatFactory.CreateNew(chatId, Channel.GeneralGaming.Id, createdAt);
        var membership = new ChatMember(chatId, userId, createdAt);

        var chats = new List<Chat> { chat }.BuildMockDbSet();
        var members = new List<ChatMember> { membership }.BuildMockDbSet();

        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _timeProviderMock.Setup(x => x.CurrentTimeUtc).Returns(readUpTo);

        var handler = new MarkChatAsRead.Handler(
            _contextMock.Object,
            _authServiceMock.Object,
            _timeProviderMock.Object);

        var command = new MarkChatAsRead.Command(chatId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        membership.LastReadAt.Should().Be(readUpTo);

        _contextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenReadUpToIsNotGreaterThanLastReadAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2);
        var currentLastReadAt = DateTimeOffset.UtcNow;
        var readUpTo = currentLastReadAt.AddMinutes(-5);

        var chat = ChatFactory.CreateNew(chatId, Channel.GeneralGaming.Id, createdAt);
        var membership = new ChatMember(chatId, userId, createdAt);
        membership.ReadUpTo(currentLastReadAt);

        var chats = new List<Chat> { chat }.BuildMockDbSet();
        var members = new List<ChatMember> { membership }.BuildMockDbSet();

        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _timeProviderMock.Setup(x => x.CurrentTimeUtc).Returns(readUpTo);

        var handler = new MarkChatAsRead.Handler(
            _contextMock.Object,
            _authServiceMock.Object,
            _timeProviderMock.Object);

        var command = new MarkChatAsRead.Command(chatId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        membership.LastReadAt.Should().Be(currentLastReadAt);

        _contextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }




}
