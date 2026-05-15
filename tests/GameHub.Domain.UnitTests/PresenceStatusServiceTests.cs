using FluentAssertions;
using GameHub.Domain.Presence;

namespace GameHub.Domain.UnitTests;

public class PresenceStatusServiceTests
{
    private readonly PresenceStatusService _service = new();

    [Fact]
    public void GetStatus_ShouldReturnOnline_WhenLastActiveWithin2Minutes()
    {
        var userId = Guid.CreateVersion7();
        var lastActive = DateTimeOffset.UtcNow;
        var currentTime = lastActive.AddMinutes(2);
        var expectedStatus = PresenceStatus.Online;
        var actualStatus = _service.GetStatus(lastActive, currentTime);
        actualStatus.Should().Be(expectedStatus);
    }

    [Fact]
    public void GetStatus_ShouldReturnAway_WhenLastActiveWithin15Minutes()
    {
        var userId = Guid.CreateVersion7();
        var lastActive = DateTimeOffset.UtcNow;
        var currentTime = lastActive.AddMinutes(15);
        var expectedStatus = PresenceStatus.Away;
        var actualStatus = _service.GetStatus(lastActive, currentTime);
        actualStatus.Should().Be(expectedStatus);
    }

    [Fact]
    public void GetStatus_ShouldReturnOffline_WhenLastActiveMoreThan15MinutesAgo()
    {
        var userId = Guid.CreateVersion7();
        var lastActive = DateTimeOffset.UtcNow;
        var currentTime = lastActive.AddMinutes(16);
        var expectedStatus = PresenceStatus.Offline;
        var actualStatus = _service.GetStatus(lastActive, currentTime);
        actualStatus.Should().Be(expectedStatus);
    }
}
