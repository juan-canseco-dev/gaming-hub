using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Features.Chats.Queries.GetParticipants;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using GameHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using System.Text;
using System.Text.Json;

namespace GameHub.Application.UnitTests.Features.Chats.Queries.GetParticipants;
public sealed class GetChatParticipantsHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Failure_When_Chat_Does_Not_Exist()
    {
        // Arrange
        var chatId = Guid.NewGuid();

        var chats = new List<Chat>().BuildMockDbSet();
        var chatMembers = new List<ChatMember>().BuildMockDbSet();
        var userProfiles = new List<UserProfile>().BuildMockDbSet();

        var context = CreateContext(
            chats.Object,
            chatMembers.Object,
            userProfiles.Object);

        var handler = new GetChatParticipants.Handler(context.Object, CreateClock(DateTimeOffset.UtcNow));
        var query = new GetChatParticipants.Query(chatId, 10, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Chat.ChatGroupNotFound");
        result.Error.Description.Should().Be($"Chat group '{chatId}' was not found.");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Cursor_Is_Invalid()
    {
        // Arrange
        var chatId = Guid.NewGuid();

        var chat = Shared.Factories.ChatTestFactory.CreateNew(chatId, 1, DateTimeOffset.UtcNow);

        var chats = new List<Chat> { chat }.BuildMockDbSet();
        var chatMembers = new List<ChatMember>().BuildMockDbSet();
        var userProfiles = new List<UserProfile>().BuildMockDbSet();

        var context = CreateContext(
            chats.Object,
            chatMembers.Object,
            userProfiles.Object);

        var handler = new GetChatParticipants.Handler(context.Object, CreateClock(DateTimeOffset.UtcNow));
        var query = new GetChatParticipants.Query(chatId, 10, "not-a-valid-base64-cursor");
        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ChatParticipants.InvalidCursor");
        result.Error.Description.Should().Be("The provided cursor is invalid.");
    }

    [Fact]
    public async Task Handle_Should_Return_First_Page_Ordered_With_Next_Cursor()
    {
        // Arrange
        var chatId = Guid.NewGuid();
        var joinedAt = DateTimeOffset.UtcNow;

        var chat = Shared.Factories.ChatTestFactory.CreateNew(chatId, 1, joinedAt);

        var user1 = new UserProfile(Guid.NewGuid(), "zoe@example.com", "zoe", "Zoe", joinedAt);
        var user2 = new UserProfile(Guid.NewGuid(), "ana@example.com", "ana", "Ana", joinedAt);
        var user3 = new UserProfile(Guid.NewGuid(), "bob@example.com", "bob", "Bob", joinedAt);

        var member1 = CreateNew(chatId, user1.Id, joinedAt);
        var member2 = CreateNew(chatId, user2.Id, joinedAt);
        var member3 = CreateNew(chatId, user3.Id, joinedAt);

        var chats = new List<Chat> { chat }.BuildMockDbSet();
        var chatMembers = new List<ChatMember> { member1, member2, member3 }.BuildMockDbSet();
        var userProfiles = new List<UserProfile> { user1, user2, user3 }.BuildMockDbSet();

        var context = CreateContext(
            chats.Object,
            chatMembers.Object,
            userProfiles.Object);

        var handler = new GetChatParticipants.Handler(context.Object, CreateClock(joinedAt));
        var query = new GetChatParticipants.Query(chatId, 2, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);

        var items = result.Value.Items.ToList();

        items[0].Username.Should().Be("ana");
        items[1].Username.Should().Be("bob");
        items.Should().OnlyContain(x => x.Presence != null && x.Presence.Status == "Online");

        result.Value.Next.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Apply_Cursor_And_Return_Next_Page()
    {
        // Arrange
        var chatId = Guid.NewGuid();
        var joinedAt = DateTimeOffset.UtcNow;

        var chat = Shared.Factories.ChatTestFactory.CreateNew(chatId, 1, joinedAt);

        var user1 = new UserProfile(Guid.NewGuid(), "ana@example.com", "ana", "Ana", joinedAt);
        var user2 = new UserProfile(Guid.NewGuid(), "bob@example.com", "bob", "Bob", joinedAt);
        var user3 = new UserProfile(Guid.NewGuid(), "zoe@example.com", "zoe", "Zoe", joinedAt);

        var member1 = CreateNew(chatId, user1.Id, joinedAt);
        var member2 = CreateNew(chatId, user2.Id, joinedAt);
        var member3 = CreateNew(chatId, user3.Id, joinedAt);

        var chats = new List<Chat> { chat }.BuildMockDbSet();
        var chatMembers = new List<ChatMember> { member1, member2, member3 }.BuildMockDbSet();
        var userProfiles = new List<UserProfile> { user1, user2, user3 }.BuildMockDbSet();

        var context = CreateContext(
            chats.Object,
            chatMembers.Object,
            userProfiles.Object);

        var cursor = EncodeCursor(joinedAt, "ana", user1.Id);

        var handler = new GetChatParticipants.Handler(context.Object, CreateClock(joinedAt));
        var query = new GetChatParticipants.Query(chatId, 2, cursor);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);

        var items = result.Value.Items.ToList();
        items[0].Username.Should().Be("bob");
        items[1].Username.Should().Be("zoe");

        result.Value.Next.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Derive_Each_Presence_Status_From_UserPresence()
    {
        var now = new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);
        var createdAt = now.AddMinutes(-20);
        var chatId = Guid.NewGuid();
        var chat = Shared.Factories.ChatTestFactory.CreateNew(chatId, 1, createdAt);
        var onlineUser = new UserProfile(Guid.NewGuid(), "online@example.com", "online", "Online", createdAt);
        var awayUser = new UserProfile(Guid.NewGuid(), "away@example.com", "away", "Away", createdAt);
        var offlineUser = new UserProfile(Guid.NewGuid(), "offline@example.com", "offline", "Offline", createdAt);

        onlineUser.Presence.Update(now.AddMinutes(-2));
        awayUser.Presence.Update(now.AddMinutes(-15));

        var users = new[] { onlineUser, awayUser, offlineUser };
        var context = CreateContext(
            new List<Chat> { chat }.BuildMockDbSet().Object,
            users.Select(x => CreateNew(chatId, x.Id, createdAt)).ToList().BuildMockDbSet().Object,
            users.ToList().BuildMockDbSet().Object);
        var handler = new GetChatParticipants.Handler(context.Object, CreateClock(now));

        var result = await handler.Handle(
            new GetChatParticipants.Query(chatId, 10, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(x =>
            x.Id == onlineUser.Id && x.Presence != null && x.Presence.Status == "Online");
        result.Value.Items.Should().ContainSingle(x =>
            x.Id == awayUser.Id && x.Presence != null && x.Presence.Status == "Away");
        result.Value.Items.Should().ContainSingle(x =>
            x.Id == offlineUser.Id && x.Presence != null && x.Presence.Status == "Offline");
    }

    private static Mock<IApplicationDbContext> CreateContext(
        DbSet<Chat> chats,
        DbSet<ChatMember> chatMembers,
        DbSet<UserProfile> userProfiles)
    {
        var context = new Mock<IApplicationDbContext>();

        context.Setup(x => x.Chats).Returns(chats);
        context.Setup(x => x.ChatMembers).Returns(chatMembers);
        context.Setup(x => x.UserProfiles).Returns(userProfiles);
        context.Setup(x => x.UserPresences).Returns(
            userProfiles.Select(x => x.Presence).ToList().BuildMockDbSet().Object);

        context.Setup(x => x.Channels).Returns(new List<Channel>().BuildMockDbSet().Object);
        context.Setup(x => x.ChatMessages).Returns(new List<ChatMessage>().BuildMockDbSet().Object);

        return context;
    }

    private static IDateTimeProvider CreateClock(DateTimeOffset currentTime) =>
        Mock.Of<IDateTimeProvider>(x => x.CurrentTimeUtc == currentTime);


    private static ChatMember CreateNew(Guid chatId, Guid userId, DateTimeOffset joinedAt)
    {
        return new ChatMember(chatId, userId, joinedAt);
    }

    private static string EncodeCursor(DateTimeOffset? lastActive, string username, Guid userId)
    {
        var json = JsonSerializer.Serialize(new TestCursor(lastActive, username, userId));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private sealed record TestCursor(DateTimeOffset? LastActive, string Username, Guid UserId);
}
