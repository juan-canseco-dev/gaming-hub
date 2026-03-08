using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using Moq;
using MockQueryable.Moq;
using System.Text;
using GameHub.Application.Features.Chats.GetMessages;
using System.Text.Json;


namespace GameHub.Application.UnitTests.Features.Chats.GetMessages;

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

        var chat = CreateChat(chatId, Channel.GeneralGaming.Id, DateTimeOffset.UtcNow);

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

        var chat = CreateChat(chatId, Channel.GeneralGaming.Id, DateTimeOffset.UtcNow.AddHours(-2));

        var message1 = new ChatMessage(
            chatId,
            userId,
            "first message",
            new DateTimeOffset(2026, 03, 08, 10, 00, 00, TimeSpan.Zero),
            ChatMessageType.User)
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

        var message2 = new ChatMessage(
            chatId,
            otherUserId,
            "second message",
            new DateTimeOffset(2026, 03, 08, 11, 00, 00, TimeSpan.Zero),
            ChatMessageType.User)
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222")
        };

        var systemMessage = new ChatMessage(
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

        result.Value.After.Should().BeNull();
        result.Value.Before.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_Paginated_Result_With_After_And_Before_When_HasMore()
    {
        // Arrange
        var chatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var chat = CreateChat(chatId, Channel.GeneralGaming.Id, DateTimeOffset.UtcNow.AddDays(-1));

        var m1 = NewMessage(chatId, userId, "message-1", new DateTimeOffset(2026, 03, 08, 15, 00, 00, TimeSpan.Zero), "00000000-0000-0000-0000-000000000004");
        var m2 = NewMessage(chatId, userId, "message-2", new DateTimeOffset(2026, 03, 08, 14, 00, 00, TimeSpan.Zero), "00000000-0000-0000-0000-000000000003");
        var m3 = NewMessage(chatId, userId, "message-3", new DateTimeOffset(2026, 03, 08, 13, 00, 00, TimeSpan.Zero), "00000000-0000-0000-0000-000000000002");
        var m4 = NewMessage(chatId, userId, "message-4", new DateTimeOffset(2026, 03, 08, 12, 00, 00, TimeSpan.Zero), "00000000-0000-0000-0000-000000000001");

        var user = new UserProfile(
            userId,
            "user@mail.com",
            "user",
            "Test User",
            DateTimeOffset.UtcNow);

        var chats = new List<Chat> { chat }.BuildMockDbSet();
        var chatMessages = new List<ChatMessage> { m1, m2, m3, m4 }.BuildMockDbSet();
        var userProfiles = new List<UserProfile> { user }.BuildMockDbSet();

        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.Chats).Returns(chats.Object);
        contextMock.Setup(x => x.ChatMessages).Returns(chatMessages.Object);
        contextMock.Setup(x => x.UserProfiles).Returns(userProfiles.Object);

        var handler = new GetMessagesByChat.Handler(contextMock.Object);
        var query = new GetMessagesByChat.Query(chatId, 3);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Items.Select(x => x.Content)
            .Should()
            .ContainInOrder("message-1", "message-2", "message-3");

        result.Value.Before.Should().NotBeNull();
        result.Value.After.Should().NotBeNull();

        var before = DecodeCursor(result.Value.Before!);
        var after = DecodeCursor(result.Value.After!);

        before.MessageId.Should().Be(m1.Id);
        before.CreatedAt.Should().Be(m1.CreatedAt);

        after.MessageId.Should().Be(m4.Id);
        after.CreatedAt.Should().Be(m4.CreatedAt);
    }

    [Fact]
    public async Task Handle_Should_Apply_Cursor_And_Return_Next_Page()
    {
        // Arrange
        var chatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var chat = CreateChat(chatId, Channel.GeneralGaming.Id, DateTimeOffset.UtcNow.AddDays(-1));

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

    private static Chat CreateChat(Guid chatId, int channelId, DateTimeOffset createdAt)
    {
        var chat = (Chat)Activator.CreateInstance(typeof(Chat), nonPublic: true)!;

        SetProperty(chat, nameof(Chat.Id), chatId);
        SetProperty(chat, nameof(Chat.ChannelId), channelId);
        SetProperty(chat, nameof(Chat.CreatedAt), createdAt);
        SetProperty(chat, nameof(Chat.Channel), Channel.FromValue(channelId)!);

        return chat;
    }

    private static ChatMessage NewMessage(
        Guid chatId,
        Guid senderUserId,
        string content,
        DateTimeOffset createdAt,
        string id)
    {
        var message = new ChatMessage(chatId, senderUserId, content, createdAt, ChatMessageType.User);
        SetProperty(message, nameof(ChatMessage.Id), Guid.Parse(id));
        return message;
    }

    private static string EncodeCursor(DateTimeOffset createdAt, Guid messageId)
    {
        var json = JsonSerializer.Serialize(new TestCursor(createdAt, messageId));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static TestCursor DecodeCursor(string encodedCursor)
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCursor));
        return JsonSerializer.Deserialize<TestCursor>(json)!;
    }

    private static void SetProperty<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        property.Should().NotBeNull($"Property '{propertyName}' was not found on type '{target.GetType().Name}'.");

        property!.SetValue(target, value);
    }

    private sealed record TestCursor(DateTimeOffset CreatedAt, Guid MessageId);
}