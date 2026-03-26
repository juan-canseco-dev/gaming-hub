using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Features.Channels.GetList;
using GameHub.Domain.Chats;
using Moq;
using MockQueryable.Moq;
using FluentAssertions;
using static GameHub.Application.UnitTests.Shared.Factories.ChatTestFactory;

namespace GameHub.Application.UnitTests.Features.Channels.Queries;

public sealed class GetChannelsHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authServiceMock;
    
    public GetChannelsHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _authServiceMock = new Mock<IAuthenticatedUserService>();
    }

    [Fact]
    public async Task Handle_Should_ReturnChannels_With_IsJoined_True_WhenUserIsMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "john_doe";

        var chat = CreateNew(
            chatId: Guid.NewGuid(),
            channelId: 1,
            createdAt: DateTimeOffset.UtcNow
        );

        chat.Join(userId, username, DateTimeOffset.UtcNow);

        
        var chatDbSetMock = new List<Chat> { chat }.BuildMockDbSet();
        var chatMembersDbSetMock = chat.Members.ToList().BuildMockDbSet();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);

        _contextMock.Setup(x => x.Chats).Returns(chatDbSetMock.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembersDbSetMock.Object);

        var query = new GetChannels.Query();
        var handler = new GetChannels.Handler(_contextMock.Object, _authServiceMock.Object);
        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var channel = result.Value.Single();

        channel.IsJoined.Should().BeTrue();
        channel.ParticipantsCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ReturnChannels_With_IsJoined_False_WhenUserIsNotMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var otherUsername = "jane_doe";

        var chat = CreateNew(
           chatId: Guid.NewGuid(),
           channelId: 1,
           createdAt: DateTimeOffset.UtcNow
       );

        chat.Join(otherUserId, otherUsername, DateTimeOffset.UtcNow);
        
        var chats = new List<Chat> { chat };
        var chatMembers = chat.Members.ToList();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);

        _contextMock.Setup(x => x.Chats).Returns(chats.BuildMockDbSet().Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembers.BuildMockDbSet().Object);

        var query = new GetChannels.Query();
        var handler = new GetChannels.Handler(_contextMock.Object, _authServiceMock.Object);
        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var channel = result.Value.Single();

        channel.IsJoined.Should().BeFalse();
        channel.ParticipantsCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_OrderChannels_By_ChannelId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var chat1 = CreateNew(
             chatId: Guid.NewGuid(),
             channelId: 1,
             createdAt: DateTimeOffset.UtcNow
        );
        var chat2 = CreateNew(
            chatId: Guid.NewGuid(),
            channelId: 2,
            createdAt: DateTimeOffset.UtcNow
        );

        var chats = new List<Chat> { chat1, chat2 };
        var chatMembers = new List<ChatMember>();

        _authServiceMock.Setup(x => x.UserId).Returns(userId);

        _contextMock.Setup(x => x.Chats).Returns(chats.BuildMockDbSet().Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(chatMembers.BuildMockDbSet().Object);

        var query = new GetChannels.Query();
        var handler = new GetChannels.Handler(_contextMock.Object, _authServiceMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var channels = result.Value.ToList();

        channels[0].Id.Should().Be(1);
        channels[1].Id.Should().Be(2);
    }
}