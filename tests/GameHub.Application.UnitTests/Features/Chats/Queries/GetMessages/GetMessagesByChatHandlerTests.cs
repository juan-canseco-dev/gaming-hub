using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using Moq;
using MockQueryable.Moq;
using System.Text;
using GameHub.Application.Features.Chats.Queries.GetMessages;
using System.Text.Json;
using static GameHub.Application.UnitTests.Shared.Helpers.ReflectionTestHelper;
using static GameHub.Application.UnitTests.Shared.Factories.ChatTestFactory;

namespace GameHub.Application.UnitTests.Features.Chats.Queries.GetMessages;

public sealed class GetMessagesByChatHandlerTests
{

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Chat_Does_Not_Exist()
    {
        // Arrange
        var chatId = Guid.NewGuid();

        var chats = new List<Chat>().BuildMockDbSet();
        var chatMessages = new List<ChatMessage>().BuildMockDbSet();
        var userProfiles = new List<UserProfile>().BuildMockDbSet();

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.Chats).Returns(chats.Object);
        contextMock.Setup(x => x.ChatMessages).Returns(chatMessages.Object);
        contextMock.Setup(x => x.UserProfiles).Returns(userProfiles.Object);

        var handler = new GetMessagesByChat.Handler(contextMock.Object);
        var query = new GetMessagesByChat.Query(chatId, 20);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeEquivalentTo(ChatErrors.ChatGroupNotFound(chatId));
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Cursor_Is_Invalid()
    {
        // Arrange
        var chatId = Guid.NewGuid();

        var chat = CreateNew(chatId, Channel.GeneralGaming.Id, DateTimeOffset.UtcNow);

        var chats = new List<Chat> { chat }.BuildMockDbSet();
        var chatMessages = new List<ChatMessage>().BuildMockDbSet();
        var userProfiles = new List<UserProfile>().BuildMockDbSet();

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.Chats).Returns(chats.Object);
        contextMock.Setup(x => x.ChatMessages).Returns(chatMessages.Object);
        contextMock.Setup(x => x.UserProfiles).Returns(userProfiles.Object);

        var handler = new GetMessagesByChat.Handler(contextMock.Object);
        var query = new GetMessagesByChat.Query(chatId, 20, "invalid-base64-cursor");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ChatMessages.InvalidCursor");
        result.Error.Description.Should().Be("The provided cursor is invalid.");
    }

    [Fact]
    public async Task Handle_Should_Return_Messages_Ordered_And_Projected_When_Chat_Exists()
    {
        // Arrange
        var chatId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var chat = CreateNew(chatId, Channel.GeneralGaming.Id, DateTimeOffset.UtcNow.AddHours(-2));

        var message1 = new ChatMessage(
            Guid.NewGuid(),
            chatId,
            userId,
            "first message",
            new DateTimeOffset(2026, 03, 08, 10, 00, 00, TimeSpan.Zero),
            ChatMessageType.User)
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

        var message2 = new ChatMessage(
            Guid.NewGuid(),
            chatId,
            otherUserId,
            "second message",
            new DateTimeOffset(2026, 03, 08, 11, 00, 00, TimeSpan.Zero),
            ChatMessageType.User)
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222")
        };

        var systemMessage = new ChatMessage(
            Guid.NewGuid(),
            chatId,
            SystemUsers.AdminUserId,
            "User joined",
            new DateTimeOffset(2026, 03, 08, 12, 00, 00, TimeSpan.Zero),
            ChatMessageType.System)
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333")
        };

        var user1 = new UserProfile(
            userId,
            "user1@mail.com",
            "user1",
            "User One",
            DateTimeOffset.UtcNow);

        var user2 = new UserProfile(
            otherUserId,
            "user2@mail.com",
            "user2",
            "User Two",
            DateTimeOffset.UtcNow);

        var chats = new List<Chat> { chat }.BuildMockDbSet();
        var chatMessages = new List<ChatMessage> { message1, message2, systemMessage }
            .BuildMockDbSet();
        var userProfiles = new List<UserProfile> { user1, user2 }
            .BuildMockDbSet();

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.Chats).Returns(chats.Object);
        contextMock.Setup(x => x.ChatMessages).Returns(chatMessages.Object);
        contextMock.Setup(x => x.UserProfiles).Returns(userProfiles.Object);

        var handler = new GetMessagesByChat.Handler(contextMock.Object);
        var query = new GetMessagesByChat.Query(chatId, 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCount(3);

        var items = result.Value.Items.ToList();

        items[0].Content.Should().Be("User joined");
        items[0].IsSystem.Should().BeTrue();
        items[0].User.Should().BeNull();

        items[1].Content.Should().Be("second message");
        items[1].IsSystem.Should().BeFalse();
        items[1].User.Should().NotBeNull();
        items[1].User!.Id.Should().Be(otherUserId);
        items[1].User.Username.Should().Be("user2");

        items[2].Content.Should().Be("first message");
        items[2].IsSystem.Should().BeFalse();
        items[2].User.Should().NotBeNull();
        items[2].User!.Id.Should().Be(userId);
        items[2].User.Username.Should().Be("user1");

        result.Value.Next.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Apply_Cursor_And_Return_Next_Page()
    {
        // Arrange
        var chatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var chat = CreateNew(chatId, Channel.GeneralGaming.Id, DateTimeOffset.UtcNow.AddDays(-1));

        var m1 = NewMessage(chatId, userId, "message-1", new DateTimeOffset(2026, 03, 08, 15, 00, 00, TimeSpan.Zero), "00000000-0000-0000-0000-000000000005");
        var m2 = NewMessage(chatId, userId, "message-2", new DateTimeOffset(2026, 03, 08, 14, 00, 00, TimeSpan.Zero), "00000000-0000-0000-0000-000000000004");
        var m3 = NewMessage(chatId, userId, "message-3", new DateTimeOffset(2026, 03, 08, 13, 00, 00, TimeSpan.Zero), "00000000-0000-0000-0000-000000000003");
        var m4 = NewMessage(chatId, userId, "message-4", new DateTimeOffset(2026, 03, 08, 12, 00, 00, TimeSpan.Zero), "00000000-0000-0000-0000-000000000002");
        var m5 = NewMessage(chatId, userId, "message-5", new DateTimeOffset(2026, 03, 08, 11, 00, 00, TimeSpan.Zero), "00000000-0000-0000-0000-000000000001");

        var user = new UserProfile(
            userId,
            "user@mail.com",
            "user",
            "Test User",
            DateTimeOffset.UtcNow);

        var chats = new List<Chat> { chat }.BuildMockDbSet();
        var chatMessages = new List<ChatMessage> { m1, m2, m3, m4, m5 }.BuildMockDbSet();
        var userProfiles = new List<UserProfile> { user }.BuildMockDbSet();

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.Chats).Returns(chats.Object);
        contextMock.Setup(x => x.ChatMessages).Returns(chatMessages.Object);
        contextMock.Setup(x => x.UserProfiles).Returns(userProfiles.Object);

        var handler = new GetMessagesByChat.Handler(contextMock.Object);

        var cursor = EncodeCursor(m3.CreatedAt, m3.Id);
        var query = new GetMessagesByChat.Query(chatId, 2, cursor);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Select(x => x.Content)
            .Should()
            .ContainInOrder("message-4", "message-5");
    }

    private static ChatMessage NewMessage(
        Guid chatId,
        Guid senderUserId,
        string content,
        DateTimeOffset createdAt,
        string id)
    {
        var message = new ChatMessage(Guid.NewGuid(), chatId, senderUserId, content, createdAt, ChatMessageType.User);
        SetProperty(message, nameof(ChatMessage.Id), Guid.Parse(id));
        return message;
    }

    private static string EncodeCursor(DateTimeOffset createdAt, Guid messageId)
    {
        var json = JsonSerializer.Serialize(new TestCursor(createdAt, messageId));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private sealed record TestCursor(DateTimeOffset CreatedAt, Guid MessageId);
}