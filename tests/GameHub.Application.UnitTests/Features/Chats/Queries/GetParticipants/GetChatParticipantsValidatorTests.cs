using FluentAssertions;
using GameHub.Application.Features.Chats.Queries.GetParticipants;

namespace GameHub.Application.UnitTests.Features.Chats.Queries.GetParticipants;

public sealed class GetChatParticipantsValidatorTests
{
    private readonly GetChatParticipants.Validator _validator = new();

    [Fact]
    public void Validate_Should_Fail_When_ChatId_Is_Empty()
    {
        // Arrange
        var query = new GetChatParticipants.Query(Guid.Empty, 10, null);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(GetChatParticipants.Query.ChatId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validate_Should_Fail_When_Limit_Is_Out_Of_Range(int limit)
    {
        // Arrange
        var query = new GetChatParticipants.Query(Guid.NewGuid(), limit, null);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.PropertyName == nameof(GetChatParticipants.Query.Limit));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void Validate_Should_Pass_When_Query_Is_Valid(int limit)
    {
        // Arrange
        var query = new GetChatParticipants.Query(Guid.NewGuid(), limit, null);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}

