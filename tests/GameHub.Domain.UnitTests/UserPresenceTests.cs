using FluentAssertions;
using GameHub.Domain.Presence;

namespace GameHub.Domain.UnitTests;

public class UserPresenceTests
{
    public static TheoryData<int, PresenceStatus> PresenceStatusCases => new()
    {
        { 2, PresenceStatus.Online },
        { 15, PresenceStatus.Away },
        { 16, PresenceStatus.Offline }
    };

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
        var wasUpdated = userPresence.Update(expectedLastActive);

        wasUpdated.Should().BeTrue();
        userPresence.LastActive.Should().Be(expectedLastActive);
    }

    [Fact]
    public void UserPresence_Update_Should_Not_Set_LastActive_When_CurrentTimeIsLesser()
    {

        var userId = Guid.CreateVersion7();
        var lastActiveAt = DateTimeOffset.UtcNow;
        var lastActiveUpdate = lastActiveAt.AddMinutes(-1);

        var userPresence = new UserPresence(userId, lastActiveAt);
        var wasUpdated = userPresence.Update(lastActiveUpdate);

        wasUpdated.Should().BeFalse();
        userPresence.LastActive.Should().Be(lastActiveAt);
    }

    [Theory]
    [MemberData(nameof(PresenceStatusCases))]
    public void UserPresence_GetStatus_Should_Return_Status_For_Elapsed_Time(
        int elapsedMinutes,
        PresenceStatus expectedStatus)
    {
        var lastActiveAt = DateTimeOffset.UtcNow;
        var userPresence = new UserPresence(Guid.CreateVersion7(), lastActiveAt);

        var status = userPresence.GetStatus(lastActiveAt.AddMinutes(elapsedMinutes));

        status.Should().Be(expectedStatus);
    }
}
