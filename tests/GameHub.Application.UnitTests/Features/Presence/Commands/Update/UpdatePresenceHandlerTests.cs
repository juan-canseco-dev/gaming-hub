using FluentAssertions;
using GameHub.Application.Abstractions.Authentication;
using GameHub.Application.Abstractions.Data;
using GameHub.Application.Abstractions.Clock;
using GameHub.Application.Features.Presence.Commands.Update;
using GameHub.Domain.Presence;
using GameHub.Domain.Users;
using GameHub.EventBus.Contracts;
using MassTransit;
using MockQueryable.Moq;
using Moq;

namespace GameHub.Application.UnitTests.Features.Presence.Commands.Update;

public sealed class UpdatePresenceHandlerTests
{
    private readonly Mock<IAuthenticatedUserService> _authService = new();
    private readonly Mock<IApplicationDbContext> _context = new();
    private readonly Mock<IDateTimeProvider> _timeProvider = new();
    private readonly Mock<IPublishEndpoint> _publisher = new();

    [Fact]
    public async Task Handle_ShouldReturnNotFound_AndHaveNoSideEffects_WhenPresenceDoesNotExist()
    {
        var userId = Guid.NewGuid();
        var presences = new List<UserPresence>().BuildMockDbSet();
        _authService.Setup(x => x.UserId).Returns(userId);
        _context.Setup(x => x.UserPresences).Returns(presences.Object);
        var sut = CreateHandler();

        var result = await sut.Handle(new UpdatePresence.Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.NotFound(userId));
        _publisher.Verify(
            x => x.Publish(It.IsAny<UserPresenceUpdateEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _context.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdatePersistAndPublish_WhenPresenceExists()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        var userId = Guid.NewGuid();
        var currentActivity = new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);
        var previousActivity = currentActivity.AddMinutes(-5);
        var presence = new UserPresence(userId, previousActivity);
        var presences = new List<UserPresence> { presence }.BuildMockDbSet();
        UserPresenceUpdateEvent? publishedEvent = null;

        _authService.Setup(x => x.UserId).Returns(userId);
        _context.Setup(x => x.UserPresences).Returns(presences.Object);
        _context.Setup(x => x.SaveChangesAsync(cancellationToken)).ReturnsAsync(1);
        _timeProvider.Setup(x => x.CurrentTimeUtc).Returns(currentActivity);
        _publisher
            .Setup(x => x.Publish(It.IsAny<UserPresenceUpdateEvent>(), cancellationToken))
            .Callback<object, CancellationToken>((message, _) => publishedEvent = (UserPresenceUpdateEvent)message)
            .Returns(Task.CompletedTask);
        var sut = CreateHandler();

        var result = await sut.Handle(new UpdatePresence.Command(), cancellationToken);

        result.IsSuccess.Should().BeTrue();
        presence.LastActive.Should().Be(currentActivity);
        publishedEvent.Should().Be(new UserPresenceUpdateEvent(userId, currentActivity));
        _publisher.Verify(x => x.Publish(It.IsAny<UserPresenceUpdateEvent>(), cancellationToken), Times.Once);
        _context.Verify(x => x.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldHaveNoSideEffects_WhenActivityIsNotNewer()
    {
        var userId = Guid.NewGuid();
        var lastActive = new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);
        var presence = new UserPresence(userId, lastActive);

        _authService.Setup(x => x.UserId).Returns(userId);
        _context.Setup(x => x.UserPresences).Returns(
            new List<UserPresence> { presence }.BuildMockDbSet().Object);
        _timeProvider.Setup(x => x.CurrentTimeUtc).Returns(lastActive);
        var sut = CreateHandler();

        var result = await sut.Handle(new UpdatePresence.Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        presence.LastActive.Should().Be(lastActive);
        _publisher.Verify(
            x => x.Publish(It.IsAny<UserPresenceUpdateEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _context.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private UpdatePresence.Handler CreateHandler() => new(
        _authService.Object,
        _context.Object,
        _timeProvider.Object,
        _publisher.Object);
}
