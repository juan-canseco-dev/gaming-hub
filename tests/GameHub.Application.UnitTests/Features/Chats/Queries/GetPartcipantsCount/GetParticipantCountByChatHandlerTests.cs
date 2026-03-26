using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Features.Chats.Queries.GetPartcipantsCount;
using GameHub.Domain.Chats;
using MockQueryable.Moq;
using Moq;
using static GameHub.Application.UnitTests.Shared.Factories.ChatTestFactory;

namespace GameHub.Application.UnitTests.Features.Chats.Queries.GetPartcipantsCount;

public class GetParticipantCountByChatHandlerTests
{

    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly GetParticipantCountByChat.Handler _handler;

    public GetParticipantCountByChatHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _handler = new GetParticipantCountByChat.Handler(_contextMock.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenChatDoesNotExist()
    {
        // Arrange
        var chatId = Guid.NewGuid();

        var chats = new List<Chat>().BuildMockDbSet();
        var members = new List<ChatMember>().BuildMockDbSet();

        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);

        var query = new GetParticipantCountByChat.Query(chatId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ChatErrors.ChatGroupNotFound(chatId));
    }

    [Fact]
    public async Task Handle_Should_ReturnParticipantsCount_WhenChatExists()
    {
        // Arrange
        var chatId = Guid.NewGuid();
        var anotherChatId = Guid.NewGuid();

        var chat = CreateNew(chatId, Channel.GeneralGaming.Id, DateTimeOffset.UtcNow);
        var anotherChat = CreateNew(anotherChatId, Channel.RetroGaming.Id, DateTimeOffset.UtcNow);

        var chats = new List<Chat>
        {
            chat,
            anotherChat
        }.BuildMockDbSet();

        var members = new List<ChatMember>
        {
            new(chatId, Guid.NewGuid(), DateTimeOffset.UtcNow),
            new(chatId, Guid.NewGuid(), DateTimeOffset.UtcNow),
            new(chatId, Guid.NewGuid(), DateTimeOffset.UtcNow),
            new(anotherChatId, Guid.NewGuid(), DateTimeOffset.UtcNow)
        }.BuildMockDbSet();

        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);

        var query = new GetParticipantCountByChat.Query(chatId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }

    [Fact]
    public async Task Handle_Should_ReturnZero_WhenChatExistsButHasNoMembers()
    {
        // Arrange
        var chatId = Guid.NewGuid();

        var chat = CreateNew(chatId, Channel.GeneralGaming.Id, DateTimeOffset.UtcNow);

        var chats = new List<Chat>
        {
            chat
        }.BuildMockDbSet();

        var members = new List<ChatMember>().BuildMockDbSet();

        _contextMock.Setup(x => x.Chats).Returns(chats.Object);
        _contextMock.Setup(x => x.ChatMembers).Returns(members.Object);

        var query = new GetParticipantCountByChat.Query(chatId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }
}
