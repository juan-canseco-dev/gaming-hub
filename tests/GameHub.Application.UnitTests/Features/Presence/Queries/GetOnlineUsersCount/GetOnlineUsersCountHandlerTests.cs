using FluentAssertions;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Abstractions.Data;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using GameHub.Domain.Presence;
using MockQueryable.Moq;
using Moq;
using GetOnlineCount = GameHub.Application.Features.Presence.Queries.GetOnlineUsersCount.GetOnlineUsersCount;

namespace GameHub.Application.UnitTests.Features.Presence.Queries.GetOnlineUsersCount;

public sealed class GetOnlineUsersCountHandlerTests
{
    private readonly Mock<IApplicationDbContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenChatDoesNotExist()
    {
        var chatId = Guid.NewGuid();
        _context.Setup(x => x.Chats).Returns(new List<Chat>().BuildMockDbSet().Object);
        var sut = new GetOnlineCount.Handler(_context.Object, _clock.Object);

        var result = await sut.Handle(new GetOnlineCount.Query(chatId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ChatErrors.ChatGroupNotFound(chatId));
    }

    [Fact]
    public async Task Handle_ShouldCountOnlyOnlineMembersOfRequestedChat()
    {
        var now = new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);
        var chat = Chat.Create(Channel.GeneralGaming.Id, now.AddHours(-1)).Value;
        var otherChatId = Guid.NewGuid();
        var onlineUserId = Guid.NewGuid();
        var boundaryUserId = Guid.NewGuid();
        var awayUserId = Guid.NewGuid();
        var otherChatUserId = Guid.NewGuid();

        _clock.Setup(x => x.CurrentTimeUtc).Returns(now);
        _context.Setup(x => x.Chats).Returns(new List<Chat> { chat }.BuildMockDbSet().Object);
        _context.Setup(x => x.ChatMembers).Returns(new List<ChatMember>
        {
            new(chat.Id, onlineUserId, now.AddHours(-1)),
            new(chat.Id, boundaryUserId, now.AddHours(-1)),
            new(chat.Id, awayUserId, now.AddHours(-1)),
            new(otherChatId, otherChatUserId, now.AddHours(-1))
        }.BuildMockDbSet().Object);
        _context.Setup(x => x.UserPresences).Returns(new List<UserPresence>
        {
            new(onlineUserId, now.AddSeconds(-30)),
            new(boundaryUserId, now.AddMinutes(-2)),
            new(awayUserId, now.AddMinutes(-3)),
            new(otherChatUserId, now)
        }.BuildMockDbSet().Object);
        var sut = new GetOnlineCount.Handler(_context.Object, _clock.Object);

        var result = await sut.Handle(new GetOnlineCount.Query(chat.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
    }
}
