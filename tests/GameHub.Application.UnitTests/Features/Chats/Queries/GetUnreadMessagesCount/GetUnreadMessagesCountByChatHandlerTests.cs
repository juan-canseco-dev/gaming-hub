using FluentAssertions;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Features.Chats.Queries.GetUnreadMessagesCount;
using GameHub.Domain.Chats;
using MockQueryable.Moq;
using Moq;
using static GameHub.Application.UnitTests.Shared.Helpers.ReflectionTestHelper;

namespace GameHub.Application.UnitTests.Features.Chats.Queries.GetUnreadMessagesCount;

public sealed class GetUnreadMessagesCountByChatHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authServiceMock;

    public GetUnreadMessagesCountByChatHandlerTests()
    {
        _contextMock = new();
        _authServiceMock = new();
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenChatDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();

        var chats = new List<Chat>()
            .BuildMockDbSet();

        var members = new List<ChatMember>()
            .BuildMockDbSet();

        var messages = new List<ChatMessage>()
            .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(messages.Object);

        var handler = new GetUnreadMessagesCountByChat.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        var query = new GetUnreadMessagesCountByChat.Query(chatId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ChatErrors.ChatGroupNotFound(chatId));
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotParticipant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var chats = new List<Chat>
        {
            CreateChat(chatId, Channel.GeneralGaming.Id, createdAt)
        }
        .BuildMockDbSet();

        var members = new List<ChatMember>()
            .BuildMockDbSet();

        var messages = new List<ChatMessage>()
            .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(messages.Object);

        var handler = new GetUnreadMessagesCountByChat.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        var query = new GetUnreadMessagesCountByChat.Query(chatId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ChatErrors.NotParticipant(userId));
    }

    [Fact]
    public async Task Handle_ShouldReturnZero_WhenChatHasNoMessages()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-1);

        var chats = new List<Chat>
        {
            CreateChat(chatId, Channel.GeneralGaming.Id, createdAt)
        }
        .BuildMockDbSet();

        var members = new List<ChatMember>
        {
            CreateChatMember(chatId, userId, createdAt, createdAt)
        }
        .BuildMockDbSet();

        var messages = new List<ChatMessage>()
            .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(messages.Object);

        var handler = new GetUnreadMessagesCountByChat.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetUnreadMessagesCountByChat.Query(chatId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnZero_WhenAllMessagesAreAlreadyRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-1);
        var lastReadAt = createdAt.AddMinutes(30);

        var chats = new List<Chat>
        {
            CreateChat(chatId, Channel.GeneralGaming.Id, createdAt)
        }
        .BuildMockDbSet();

        var members = new List<ChatMember>
        {
            CreateChatMember(chatId, userId, createdAt, lastReadAt)
        }
        .BuildMockDbSet();

        var messages = new List<ChatMessage>
        {
            CreateChatMessage(Guid.NewGuid(), chatId, otherUserId, lastReadAt.AddMinutes(-5)),
            CreateChatMessage(Guid.NewGuid(), chatId, otherUserId, lastReadAt)
        }
        .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(messages.Object);

        var handler = new GetUnreadMessagesCountByChat.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetUnreadMessagesCountByChat.Query(chatId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldCountOnlyUnreadMessages_ForRequestedChat()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var otherChatId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-1);
        var lastReadAt = createdAt.AddMinutes(10);

        var chats = new List<Chat>
        {
            CreateChat(chatId, Channel.GeneralGaming.Id, createdAt),
            CreateChat(otherChatId, Channel.RetroGaming.Id, createdAt)
        }
        .BuildMockDbSet();

        var members = new List<ChatMember>
        {
            CreateChatMember(chatId, userId, createdAt, lastReadAt)
        }
        .BuildMockDbSet();

        var messages = new List<ChatMessage>
        {
            CreateChatMessage(Guid.NewGuid(), chatId, otherUserId, createdAt.AddMinutes(9)),   // read
            CreateChatMessage(Guid.NewGuid(), chatId, otherUserId, createdAt.AddMinutes(11)),  // unread
            CreateChatMessage(Guid.NewGuid(), chatId, otherUserId, createdAt.AddMinutes(12)),  // unread
            CreateChatMessage(Guid.NewGuid(), otherChatId, otherUserId, createdAt.AddMinutes(20)) // other chat
        }
        .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(messages.Object);

        var handler = new GetUnreadMessagesCountByChat.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetUnreadMessagesCountByChat.Query(chatId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldExcludeMessagesSentByCurrentUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-1);
        var lastReadAt = createdAt.AddMinutes(10);

        var chats = new List<Chat>
        {
            CreateChat(chatId, Channel.GeneralGaming.Id, createdAt)
        }
        .BuildMockDbSet();

        var members = new List<ChatMember>
        {
            CreateChatMember(chatId, userId, createdAt, lastReadAt)
        }
        .BuildMockDbSet();

        var messages = new List<ChatMessage>
        {
            CreateChatMessage(Guid.NewGuid(), chatId, userId,      createdAt.AddMinutes(11)),
            CreateChatMessage(Guid.NewGuid(), chatId, otherUserId, createdAt.AddMinutes(12)),
            CreateChatMessage(Guid.NewGuid(), chatId, otherUserId, createdAt.AddMinutes(13))
        }
        .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(messages.Object);

        var handler = new GetUnreadMessagesCountByChat.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetUnreadMessagesCountByChat.Query(chatId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldIgnoreMessagesFromOtherChats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var otherChatId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-1);
        var lastReadAt = createdAt.AddMinutes(10);

        var chats = new List<Chat>
        {
            CreateChat(chatId, Channel.GeneralGaming.Id, createdAt),
            CreateChat(otherChatId, Channel.RetroGaming.Id, createdAt)
        }
        .BuildMockDbSet();

        var members = new List<ChatMember>
        {
            CreateChatMember(chatId, userId, createdAt, lastReadAt)
        }
        .BuildMockDbSet();

        var messages = new List<ChatMessage>
        {
            CreateChatMessage(Guid.NewGuid(), otherChatId, otherUserId, createdAt.AddMinutes(11)),
            CreateChatMessage(Guid.NewGuid(), otherChatId, otherUserId, createdAt.AddMinutes(12)),
            CreateChatMessage(Guid.NewGuid(), chatId, otherUserId, createdAt.AddMinutes(13))
        }
        .BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);
        _contextMock.Setup(x => x.ChatMessages).Returns(messages.Object);

        var handler = new GetUnreadMessagesCountByChat.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetUnreadMessagesCountByChat.Query(chatId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
    }

    private static Chat CreateChat(Guid chatId, int channelId, DateTimeOffset createdAt)
    {
        var chat = (Chat)Activator.CreateInstance(typeof(Chat), nonPublic: true)!;

        SetProperty(chat, nameof(Chat.Id), chatId);
        SetProperty(chat, nameof(Chat.ChannelId), channelId);
        SetProperty(chat, nameof(Chat.CreatedAt), createdAt);
        SetProperty(chat, nameof(Chat.Channel), Channel.FromValue(channelId)!);

        return chat;
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