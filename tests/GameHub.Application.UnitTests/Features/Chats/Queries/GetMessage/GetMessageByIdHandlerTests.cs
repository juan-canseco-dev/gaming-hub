using FluentAssertions;
using GameHub.Application.Abstractions.Data;
using GameHub.Domain.Chats;
using GameHub.Domain.Users;
using MockQueryable.Moq;
using Moq;
using GameHub.Application.Features.Chats.Queries.GetMessage;
using static GameHub.Application.UnitTests.Shared.Helpers.ReflectionTestHelper;

namespace GameHub.Application.UnitTests.Features.Chats.Queries.GetMessage;

public sealed class GetMessageByIdHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnMessageDto_WhenMessageExistsAndUserExists()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var chatId = Guid.NewGuid();
        var senderUserId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 03, 08, 10, 30, 00, TimeSpan.Zero);

        var message = new ChatMessage(
            Guid.NewGuid(),
            chatId,
            senderUserId,
            "Hello world",
            createdAt,
            ChatMessageType.User);

        SetProperty(message, "Id", messageId);

        var user = new UserProfile(
            senderUserId,
            "juan@example.com",
            "juanc",
            "Juan Canseco",
            createdAt);

        var messages = new List<ChatMessage> { message };
        var users = new List<UserProfile> { user };

        var chatMessagesDbSetMock = messages.BuildMockDbSet();
        var userProfilesDbSetMock = users.BuildMockDbSet();

        var contextMock = new Mock<IApplicationDbContext>();

        contextMock.Setup(x => x.ChatMessages).Returns(chatMessagesDbSetMock.Object);
        contextMock.Setup(x => x.UserProfiles).Returns(userProfilesDbSetMock.Object);

        var handler = new GetMessageById.Handler(contextMock.Object);
        var query = new GetMessageById.Query(messageId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        result.Value.Id.Should().Be(messageId);
        result.Value.Content.Should().Be("Hello world");
        result.Value.CreatedAt.Should().Be(createdAt.UtcDateTime);
        result.Value.IsSystem.Should().BeFalse();

        result.Value.User.Should().NotBeNull();
        result.Value.User!.Id.Should().Be(senderUserId);
        result.Value.User.Username.Should().Be("juanc");
        result.Value.User.Email.Should().Be("juan@example.com");
        result.Value.User.Fullname.Should().Be("Juan Canseco");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenMessageDoesNotExist()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        var messages = new List<ChatMessage>();
        var users = new List<UserProfile>();

        var chatMessagesDbSetMock = messages.BuildMockDbSet();
        var userProfilesDbSetMock = users.BuildMockDbSet();

        var contextMock = new Mock<IApplicationDbContext>();

        contextMock.Setup(x => x.ChatMessages).Returns(chatMessagesDbSetMock.Object);
        contextMock.Setup(x => x.UserProfiles).Returns(userProfilesDbSetMock.Object);

        var handler = new GetMessageById.Handler(contextMock.Object);
        var query = new GetMessageById.Query(messageId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(MessageErrors.NotFound(messageId));
    }
}
