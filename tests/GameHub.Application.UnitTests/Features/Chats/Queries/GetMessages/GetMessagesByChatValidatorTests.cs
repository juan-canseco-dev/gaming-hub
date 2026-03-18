using FluentAssertions;
using GameHub.Application.Features.Chats.Queries.GetMessages;

namespace GameHub.Application.UnitTests.Features.Chats.Queries.GetMessages;

public sealed class GetMessagesByChatValidatorTests
{
    private readonly GetMessagesByChat.Validator _validator = new();

    [Fact]
    public void Validate_Should_Have_Error_When_ChatId_Is_Empty()
    {
        // Arrange
        var query = new GetMessagesByChat.Query(
            Guid.Empty,
            GetMessagesByChat.Validator.MinLimit);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(GetMessagesByChat.Query.ChatId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_Have_Error_When_Limit_Is_Less_Than_Minimum(int limit)
    {
        // Arrange
        var query = new GetMessagesByChat.Query(
            Guid.NewGuid(),
            limit);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(GetMessagesByChat.Query.Limit));
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    public void Validate_Should_Have_Error_When_Limit_Is_Greater_Than_Maximum(int limit)
    {
        // Arrange
        var query = new GetMessagesByChat.Query(
            Guid.NewGuid(),
            limit);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(GetMessagesByChat.Query.Limit));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_Should_Pass_When_Query_Is_Valid(int limit)
    {
        // Arrange
        var query = new GetMessagesByChat.Query(
            Guid.NewGuid(),
            limit);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}