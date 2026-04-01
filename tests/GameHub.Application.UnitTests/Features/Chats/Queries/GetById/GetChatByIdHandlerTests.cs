using FluentAssertions;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Features.Chats.Queries.GetById;
using GameHub.Domain.Chats;
using MockQueryable.Moq;
using Moq;
using System.Reflection;

namespace GameHub.Application.UnitTests.Features.Chats.Queries.GetById;

public sealed class GetChatByIdHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authServiceMock;
    private readonly MessagePreviewService _previewService;

    public GetChatByIdHandlerTests()
    {
        _contextMock = new();
        _authServiceMock = new();
        _previewService = new();
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Chat_Not_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var chatId = Guid.NewGuid();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.ChatMembers)
            .Returns(new List<ChatMember>().BuildMockDbSet().Object);

        var handler = new GetChatById.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetChatById.Query(chatId),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ChatErrors.ChatGroupNotFound(chatId));
    }

    [Fact]
    public async Task Handle_Should_Return_Chat_When_User_Is_Participant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        _authServiceMock.Setup(x => x.UserId).Returns(userId);

        var membership = CreateChatMember(
            userId,
            otherUserId,
            Channel.GeneralGaming,
            now,
            "hello");

        _contextMock.Setup(x => x.ChatMembers)
            .Returns(new List<ChatMember> { membership }.BuildMockDbSet().Object);

        var handler = new GetChatById.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetChatById.Query(membership.ChatId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(membership.ChatId);
    }

    [Fact]
    public async Task Handle_Should_Map_ChatDto_Correctly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        _authServiceMock.Setup(x => x.UserId).Returns(userId);

        var membership = CreateChatMember(
            userId,
            otherUserId,
            Channel.RpgAndStory,
            now,
            "last message");

        membership.ReadUpTo(now.AddMinutes(-10));

        _contextMock.Setup(x => x.ChatMembers)
            .Returns(new List<ChatMember> { membership }.BuildMockDbSet().Object);

        var handler = new GetChatById.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetChatById.Query(membership.ChatId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var dto = result.Value;

        dto.Id.Should().Be(membership.ChatId);
        dto.ChannelId.Should().Be(Channel.RpgAndStory.Id);
        dto.Slug.Should().Be(Channel.RpgAndStory.Slug);
        dto.Name.Should().Be(Channel.RpgAndStory.Name);
        dto.Description.Should().Be(Channel.RpgAndStory.Description);
        dto.ParticipantsCount.Should().Be(2);
        dto.LastMesagePreview.Should().Be("last message");
        dto.LastMessageAt.Should().Be(now);
        dto.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_Calculate_Unread_Count_When_LastReadAt_Is_Null()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        _authServiceMock.Setup(x => x.UserId).Returns(userId);

        var chat = CreateChat(Channel.IndieAndCreative, now.AddHours(-1));

        chat.Join(userId, "user1", now.AddMinutes(-20));
        chat.Join(otherUserId, "user2", now.AddMinutes(-20));

        WireUpChatMembers(chat);

        var member = chat.Members.First(x => x.UserId == userId);

        // LastReadAt = null (default)

        chat.AddMessage(otherUserId, "msg1", now.AddMinutes(-5), _previewService);
        chat.AddMessage(userId, "mine", now.AddMinutes(-3), _previewService);

        _contextMock.Setup(x => x.ChatMembers)
            .Returns(new List<ChatMember> { member }.BuildMockDbSet().Object);

        var handler = new GetChatById.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        // Act
        var result = await handler.Handle(
            new GetChatById.Query(chat.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UnreadCount.Should().Be(3); // only other user's message
    }

    // =========================
    // Helpers (IDENTICAL STYLE)
    // =========================

    private ChatMember CreateChatMember(
        Guid currentUserId,
        Guid otherUserId,
        Channel channel,
        DateTimeOffset messageTime,
        string messageContent)
    {
        var chat = CreateChat(channel, messageTime.AddHours(-1));

        chat.Join(currentUserId, "user1", messageTime.AddMinutes(-10));
        chat.Join(otherUserId, "user2", messageTime.AddMinutes(-10));

        WireUpChatMembers(chat);

        chat.AddMessage(otherUserId, messageContent, messageTime, _previewService);

        return chat.Members.First(x => x.UserId == currentUserId);
    }

    private static Chat CreateChat(Channel channel, DateTimeOffset createdAt)
    {
        var result = Chat.Create(channel.Id, createdAt);
        result.IsSuccess.Should().BeTrue();

        var chat = result.Value;

        SetChatId(chat, Guid.NewGuid());
        SetChatChannel(chat, channel);

        return chat;
    }

    private static void WireUpChatMembers(Chat chat)
    {
        foreach (var member in chat.Members)
        {
            SetChatMemberChat(member, chat);
        }
    }

    private static void SetChatId(Chat chat, Guid chatId)
    {
        var property = typeof(Chat).GetProperty(
            nameof(Chat.Id),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property!.SetValue(chat, chatId);
    }

    private static void SetChatChannel(Chat chat, Channel channel)
    {
        var property = typeof(Chat).GetProperty(
            nameof(Chat.Channel),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property!.SetValue(chat, channel);
    }

    private static void SetChatMemberChat(ChatMember member, Chat chat)
    {
        var property = typeof(ChatMember).GetProperty(
            nameof(ChatMember.Chat),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property!.SetValue(member, chat);
    }
}
