using FluentAssertions;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Features.Chats.Queries.GetTotalUnreadMessagesCount;
using GameHub.Domain.Chats;
using MockQueryable.Moq;
using Moq;
using static GameHub.Application.UnitTests.Shared.Helpers.ReflectionTestHelper;

namespace GameHub.Application.UnitTests.Features.Chats.Queries.GetTotalUnreadMessagesCount;

public sealed class GetTotalChatUnreadMessagesCountHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authServiceMock;

    public GetTotalChatUnreadMessagesCountHandlerTests()
    {
        _contextMock = new();
        _authServiceMock = new();
    }

    [Fact]
    public async Task Handle_ShouldReturnZero_WhenUserHasNoChatMemberships()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var chatMembers = new List<ChatMember>()
            .BuildMockDbSet();

        var chatMessages = new List<ChatMessage>
        {
            CreateChatMessage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-1))
        }
        .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembers.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(chatMessages.Object);

        var handler = new GetTotalChatUnreadMessagesCount.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetTotalChatUnreadMessagesCount.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnZero_WhenAllMessagesAreAlreadyRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var lastReadAt = DateTimeOffset.UtcNow;

        var member = CreateChatMember(chatId, userId, createdAt: lastReadAt.AddHours(-1), lastReadAt: lastReadAt);

        var messages = new List<ChatMessage>
        {
            CreateChatMessage(Guid.NewGuid(), chatId, Guid.NewGuid(), lastReadAt.AddMinutes(-10)),
            CreateChatMessage(Guid.NewGuid(), chatId, Guid.NewGuid(), lastReadAt)
        };

        var chatMembers = new List<ChatMember> { member }
            .BuildMockDbSet();

        var chatMessages = messages
            .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembers.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(chatMessages.Object);

        var handler = new GetTotalChatUnreadMessagesCount.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetTotalChatUnreadMessagesCount.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldExcludeMessagesSentByCurrentUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var lastReadAt = DateTimeOffset.UtcNow.AddMinutes(-30);

        var member = CreateChatMember(chatId, userId, createdAt: lastReadAt.AddHours(-1), lastReadAt: lastReadAt);

        var messages = new List<ChatMessage>
        {
            CreateChatMessage(Guid.NewGuid(), chatId, userId, lastReadAt.AddMinutes(1)),
            CreateChatMessage(Guid.NewGuid(), chatId, otherUserId, lastReadAt.AddMinutes(2)),
            CreateChatMessage(Guid.NewGuid(), chatId, otherUserId, lastReadAt.AddMinutes(3))
        };

        var chatMembers = new List<ChatMember> { member }
            .BuildMockDbSet();

        var chatMessages = messages
            .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembers.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(chatMessages.Object);

        var handler = new GetTotalChatUnreadMessagesCount.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetTotalChatUnreadMessagesCount.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldCountOnlyUnreadMessages_FromChatsWhereUserIsMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var joinedChat1 = Guid.NewGuid();
        var joinedChat2 = Guid.NewGuid();
        var notJoinedChat = Guid.NewGuid();

        var baseTime = DateTimeOffset.UtcNow.AddHours(-1);

        var members = new List<ChatMember>
        {
            CreateChatMember(joinedChat1, userId, baseTime.AddHours(-1), baseTime.AddMinutes(10)),
            CreateChatMember(joinedChat2, userId, baseTime.AddHours(-1), baseTime.AddMinutes(20))
        };

        var messages = new List<ChatMessage>
        {
            // joinedChat1
            CreateChatMessage(Guid.NewGuid(), joinedChat1, otherUserId, baseTime.AddMinutes(5)),   // read
            CreateChatMessage(Guid.NewGuid(), joinedChat1, otherUserId, baseTime.AddMinutes(11)),  // unread
            CreateChatMessage(Guid.NewGuid(), joinedChat1, userId,      baseTime.AddMinutes(12)),  // own, exclude

            // joinedChat2
            CreateChatMessage(Guid.NewGuid(), joinedChat2, otherUserId, baseTime.AddMinutes(19)),  // read
            CreateChatMessage(Guid.NewGuid(), joinedChat2, otherUserId, baseTime.AddMinutes(21)),  // unread
            CreateChatMessage(Guid.NewGuid(), joinedChat2, otherUserId, baseTime.AddMinutes(22)),  // unread

            // not joined chat
            CreateChatMessage(Guid.NewGuid(), notJoinedChat, otherUserId, baseTime.AddMinutes(50)) // exclude
        };

        var chatMembers = members
            .BuildMockDbSet();

        var chatMessages = messages
            .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembers.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(chatMessages.Object);

        var handler = new GetTotalChatUnreadMessagesCount.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetTotalChatUnreadMessagesCount.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnZero_WhenThereAreNoMessages()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();

        var members = new List<ChatMember>
        {
            CreateChatMember(chatId, userId, DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow.AddHours(-1))
        };

        var chatMembers = members
            .BuildMockDbSet();

        var chatMessages = new List<ChatMessage>()
            .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembers.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(chatMessages.Object);

        var handler = new GetTotalChatUnreadMessagesCount.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetTotalChatUnreadMessagesCount.Query(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    private static ChatMember CreateChatMember(
        Guid chatId,
        Guid userId,
        DateTimeOffset createdAt,
        DateTimeOffset lastReadAt)
    {
        var member = new ChatMember(chatId, userId, createdAt);
        member.ReadUpTo(lastReadAt);
        return member;
    }

    private static ChatMessage CreateChatMessage(
        Guid messageId,
        Guid chatId,
        Guid senderUserId,
        DateTimeOffset createdAt)
    {
        var message = (ChatMessage)Activator.CreateInstance(typeof(ChatMessage), nonPublic: true)!;

        SetProperty(message, nameof(ChatMessage.Id), messageId);
        SetProperty(message, nameof(ChatMessage.ChatId), chatId);
        SetProperty(message, nameof(ChatMessage.SenderUserId), senderUserId);
        SetProperty(message, nameof(ChatMessage.CreatedAt), createdAt);

        return message;
    }

}
