using GameHub.Application.Abstractions.Realtime.Chats;
using GameHub.Contracts.Chats;
using GameHub.Application.Features.Chats.Consumers;
using GameHub.Application.Features.Chats.Queries.GetMessage;
using GameHub.Application.Features.Chats.Queries.GetParticipantsCount;
using GameHub.Abstractions.Primitives;
using GameHub.Contracts.Notifications;
using GameHub.Domain.Chats;
using GameHub.Domain.Channels;
using GameHub.EventBus.Contracts;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameHub.Application.UnitTests.Features.Chats.Consumers;

public class ChatMemberJoinedConsumerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IUserJoinedChatNotifier> _notifierMock;
    private readonly Mock<ILogger<ChatMemberJoinedConsumer>> _loggerMock;

    public ChatMemberJoinedConsumerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _notifierMock = new Mock<IUserJoinedChatNotifier>();
        _loggerMock = new Mock<ILogger<ChatMemberJoinedConsumer>>();
    }

    [Fact]
    public async Task Consume_ShouldReturnWithoutNotification_WhenParticipantCountQueryFails()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new ChatMemberJoinedEvent
        {
            ChatId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            MessageId = Guid.NewGuid()
        };
        var cancellationToken = new CancellationTokenSource().Token;
        var contextMock = CreateContextMock(message, cancellationToken);

        var expectedError = ChatErrors.ChatGroupNotFound(message.ChatId);

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<GetParticipantCountByChat.Query>(q => q.ChatId == message.ChatId),
                cancellationToken))
            .ReturnsAsync(Result.Failure<int>(expectedError));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        _mediatorMock.Verify(x => x.Send(
                It.Is<GetParticipantCountByChat.Query>(q => q.ChatId == message.ChatId),
                cancellationToken),
            Times.Once);

        _mediatorMock.Verify(x => x.Send(
                It.IsAny<GetMessageById.Query>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _notifierMock.Verify(x => x.NotifyAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserJoinedNotification>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_ShouldReturnWithoutNotification_WhenGetMessageQueryFails()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new ChatMemberJoinedEvent
        {
            ChatId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            MessageId = Guid.NewGuid()
        };
        var cancellationToken = new CancellationTokenSource().Token;
        var contextMock = CreateContextMock(message, cancellationToken);

        var expectedError = MessageErrors.NotFound(message.MessageId);

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<GetParticipantCountByChat.Query>(q => q.ChatId == message.ChatId),
                cancellationToken))
            .ReturnsAsync(Result.Success(5));

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<GetMessageById.Query>(q => q.MessageId == message.MessageId),
                cancellationToken))
            .ReturnsAsync(Result.Failure<MessageDto>(expectedError));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        _mediatorMock.Verify(x => x.Send(
                It.Is<GetParticipantCountByChat.Query>(q => q.ChatId == message.ChatId),
                cancellationToken),
            Times.Once);

        _mediatorMock.Verify(x => x.Send(
                It.Is<GetMessageById.Query>(q => q.MessageId == message.MessageId),
                cancellationToken),
            Times.Once);

        _notifierMock.Verify(x => x.NotifyAsync(
                It.IsAny<Guid>(),
                It.IsAny<UserJoinedNotification>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_ShouldNotify_WhenParticipantCountAndMessageQueriesSucceed()
    {
        // Arrange
        var consumer = CreateConsumer();
        var message = new ChatMemberJoinedEvent
        {
            ChatId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            MessageId = Guid.NewGuid()
        };
        var cancellationToken = new CancellationTokenSource().Token;
        var contextMock = CreateContextMock(message, cancellationToken);

        var participantCount = 7;
        var messageDto = new MessageDto
        {
            Id = message.MessageId,
            Content = "User joined the chat.",
            CreatedAt = DateTime.UtcNow,
            IsSystem = true
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<GetParticipantCountByChat.Query>(q => q.ChatId == message.ChatId),
                cancellationToken))
            .ReturnsAsync(Result.Success(participantCount));

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<GetMessageById.Query>(q => q.MessageId == message.MessageId),
                cancellationToken))
            .ReturnsAsync(Result.Success(messageDto));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        _mediatorMock.Verify(x => x.Send(
                It.Is<GetParticipantCountByChat.Query>(q => q.ChatId == message.ChatId),
                cancellationToken),
            Times.Once);

        _mediatorMock.Verify(x => x.Send(
                It.Is<GetMessageById.Query>(q => q.MessageId == message.MessageId),
                cancellationToken),
            Times.Once);

        _notifierMock.Verify(x => x.NotifyAsync(
                message.ChatId,
                It.Is<UserJoinedNotification>(n =>
                    n.NumberOfParticipants == participantCount &&
                    n.Message == messageDto),
                cancellationToken),
            Times.Once);
    }

    private ChatMemberJoinedConsumer CreateConsumer()
    {
        return new ChatMemberJoinedConsumer(
            _mediatorMock.Object,
            _notifierMock.Object,
            _loggerMock.Object);
    }

    private static Mock<ConsumeContext<ChatMemberJoinedEvent>> CreateContextMock(
        ChatMemberJoinedEvent message,
        CancellationToken cancellationToken)
    {
        var contextMock = new Mock<ConsumeContext<ChatMemberJoinedEvent>>();

        contextMock.SetupGet(x => x.Message).Returns(message);
        contextMock.SetupGet(x => x.CancellationToken).Returns(cancellationToken);

        return contextMock;
    }
}