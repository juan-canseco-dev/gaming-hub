using FluentAssertions;
using GameHub.Domain.Presence;

namespace GameHub.Domain.UnitTests;

public class UserPresenceTests
{
    [Fact]
    public void UserPresence_Properties_Should_Be_Set_Correctly()
    {
        var userId = Guid.CreateVersion7();
        var lastActiveAt = DateTimeOffset.UtcNow;

        var userPresence = new UserPresence(userId, lastActiveAt);
        userPresence.UserId.Should().Be(userId);
        userPresence.LastActive.Should().Be(lastActiveAt);

    }

    [Fact]
    public void UserPresence_Update_Should_Be_Set_LastActive_Correctly_When_CurrentTimeIsGreater()
    {
        var userId = Guid.CreateVersion7();
        var lastActiveAt = DateTimeOffset.UtcNow;
        var expectedLastActive = lastActiveAt.AddMinutes(1);

        var userPresence = new UserPresence(userId, lastActiveAt);
        userPresence.Update(expectedLastActive);

        userPresence.LastActive.Should().Be(expectedLastActive);
    }

    [Fact]
    public void UserPresence_Update_Should_Not_Set_LastActive_When_CurrentTimeIsLesser()
    {

        var userId = Guid.CreateVersion7();
        var lastActiveAt = DateTimeOffset.UtcNow;
        var lastActiveUpdate = lastActiveAt.AddMinutes(-1);

        var userPresence = new UserPresence(userId, lastActiveAt);
        userPresence.Update(lastActiveUpdate);

        userPresence.LastActive.Should().Be(lastActiveAt);
    }
}
