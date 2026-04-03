using FluentAssertions;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Features.Chats.Queries.GetMyChats;
using GameHub.Domain.Chats;
using MockQueryable.Moq;
using Moq;
using System.Reflection;

namespace GameHub.Application.UnitTests.Features.Chats.Queries.MyChats;

public sealed class GetUserChatsHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authServiceMock;
    private readonly MessagePreviewService _previewService;
    
    public GetUserChatsHandlerTests()
    {
        _contextMock = new();
        _authServiceMock = new();
        _previewService = new();
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_When_User_Has_No_Chats()
    {
        var userId = Guid.NewGuid();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);
        _contextMock.Setup(x => x.ChatMembers)
            .Returns(new List<ChatMember>().BuildMockDbSet().Object);

        var handler = new GetUserChats.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        var result = await handler.Handle(new GetUserChats.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Return_User_Chats_Ordered_By_LastMessageAt_Descending()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        _authServiceMock.Setup(x => x.UserId).Returns(userId);

        var older = CreateChatMember(
            currentUserId: userId,
            otherUserId: otherUserId,
            channel: Channel.GeneralGaming,
            messageTime: now.AddMinutes(-30),
            messageContent: "older message");

        var newer = CreateChatMember(
            currentUserId: userId,
            otherUserId: otherUserId,
            channel: Channel.CompetitivePlay,
            messageTime: now,
            messageContent: "newer message");

        _contextMock.Setup(x => x.ChatMembers)
            .Returns(new List<ChatMember> { older, newer }.BuildMockDbSet().Object);

        var handler = new GetUserChats.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        var result = await handler.Handle(new GetUserChats.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(x => x.Name)
            .Should()
            .ContainInOrder("Competitive Play", "General Gaming");
    }

    [Fact]
    public async Task Handle_Should_Map_ChatDto_Correctly()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        _authServiceMock.Setup(x => x.UserId).Returns(userId);

        var membership = CreateChatMember(
            currentUserId: userId,
            otherUserId: otherUserId,
            channel: Channel.RpgAndStory,
            messageTime: now,
            messageContent: "last message");

        membership.ReadUpTo(now.AddMinutes(-10));

        _contextMock.Setup(x => x.ChatMembers)
            .Returns(new List<ChatMember> { membership }.BuildMockDbSet().Object);

        var handler = new GetUserChats.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        var result = await handler.Handle(new GetUserChats.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var dto = result.Value.Single();
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
    public async Task Handle_Should_Calculate_Unread_Count_Correctly()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        _authServiceMock.Setup(x => x.UserId).Returns(userId);

        var chat = CreateChat(Channel.IndieAndCreative, now.AddHours(-1));

        chat.Join(userId, "user1", now.AddMinutes(-20));
        chat.Join(otherUserId, "user2", now.AddMinutes(-20));

        WireUpChatMembers(chat);

        var member = chat.Members.First(x => x.UserId == userId);
        member.ReadUpTo(now.AddMinutes(-10));

        chat.AddMessage(otherUserId, "msg1", now.AddMinutes(-5), _previewService);
        chat.AddMessage(otherUserId, "msg2", now.AddMinutes(-4), _previewService);
        chat.AddMessage(otherUserId, "old", now.AddMinutes(-15), _previewService);
        chat.AddMessage(userId, "mine", now.AddMinutes(-3), _previewService);

        _contextMock.Setup(x => x.ChatMembers)
            .Returns(new List<ChatMember> { member }.BuildMockDbSet().Object);

        var handler = new GetUserChats.Handler(
            _contextMock.Object,
            _authServiceMock.Object);

        var result = await handler.Handle(new GetUserChats.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Single().UnreadCount.Should().Be(2);
    }

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

        property.Should().NotBeNull();
        property!.SetValue(chat, chatId);
    }

    private static void SetChatChannel(Chat chat, Channel channel)
    {
        var property = typeof(Chat).GetProperty(
            nameof(Chat.Channel),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property.Should().NotBeNull();
        property!.SetValue(chat, channel);
    }

    private static void SetChatMemberChat(ChatMember member, Chat chat)
    {
        var property = typeof(ChatMember).GetProperty(
            nameof(ChatMember.Chat),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property.Should().NotBeNull();
        property!.SetValue(member, chat);
    }
}