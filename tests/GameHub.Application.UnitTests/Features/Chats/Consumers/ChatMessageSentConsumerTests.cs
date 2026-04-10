using GameHub.Application.Abstractions.Realtime.Chats;
using GameHub.Contracts.Chats;
using GameHub.Contracts.Profile;
using GameHub.Application.Features.Chats.Consumers;
using GameHub.Application.Features.Chats.Queries.GetMessage;
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

public class ChatMessageSentConsumerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IMessageSentNotifier> _notifierMock;
    private readonly Mock<ILogger<ChatMessageSentConsumer>> _loggerMock;

    public ChatMessageSentConsumerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _notifierMock = new Mock<IMessageSentNotifier>();
        _loggerMock = new Mock<ILogger<ChatMessageSentConsumer>>();
    }

    [Fact]
    public async Task Consume_ShouldNotify_WhenGetMessageByIdSucceeds()
    {
        // Arrange
        var consumer = CreateConsumer();
        var chatId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;

        var eventMessage = new ChatMessageSentEvent
        {
            UserId = Guid.NewGuid(),
            ChatId = chatId,
            MessageId = messageId
        };

        var messageDto = new MessageDto
        {
            Id = messageId,
            Content = "Test message",
            CreatedAt = DateTime.UtcNow,
            IsSystem = false,
            User = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "juan",
                Email = "juan@test.com",
                Fullname = "Juan Canseco"
            }
        };

        var contextMock = CreateContextMock(eventMessage, cancellationToken);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetMessageById.Query>(), cancellationToken))
            .ReturnsAsync(Result.Success(messageDto));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        _notifierMock.Verify(
            x => x.NotifyAsync(
                chatId,
                It.Is<MessageNotification>(n => n.Message == messageDto),
                cancellationToken),
            Times.Once);
    }


    [Fact]
    public async Task Consume_ShouldNotNotify_WhenGetMessageByIdFails()
    {
        // Arrange
        var consumer = CreateConsumer();
        var chatId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;

        var eventMessage = new ChatMessageSentEvent
        {
            UserId = Guid.NewGuid(),
            ChatId = chatId,
            MessageId = messageId
        };

        var error = MessageErrors.NotFound(messageId);
        var contextMock = CreateContextMock(eventMessage, cancellationToken);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetMessageById.Query>(), cancellationToken))
            .ReturnsAsync(Result.Failure<MessageDto>(error));

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        _notifierMock.Verify(
            x => x.NotifyAsync(
                It.IsAny<Guid>(),
                It.IsAny<MessageNotification>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
 
    private ChatMessageSentConsumer CreateConsumer()
    {
        return new ChatMessageSentConsumer(
            _mediatorMock.Object,
            _notifierMock.Object,
            _loggerMock.Object);
    }

    private static Mock<ConsumeContext<ChatMessageSentEvent>> CreateContextMock(
        ChatMessageSentEvent message,
        CancellationToken cancellationToken)
    {
        var contextMock = new Mock<ConsumeContext<ChatMessageSentEvent>>();

        contextMock.SetupGet(x => x.Message).Returns(message);
        contextMock.SetupGet(x => x.CancellationToken).Returns(cancellationToken);

        return contextMock;
    }
}