using FluentValidation.TestHelper;
using GameHub.Application.Features.Chats.Join;

namespace GameHub.Application.UnitTests.Features.Chats.Join;

public sealed class JoinChatValidatorTests
{
    private readonly JoinChat.Validator _validator;

    public JoinChatValidatorTests()
    {
        _validator = new JoinChat.Validator();
    }

    [Fact]
    public void Should_Have_Error_When_ChatId_Is_Empty()
    {
        // Arrange
        var command = new JoinChat.Command(
            Guid.Empty,
            Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChatId);
    }

    [Fact]
    public void Should_Have_Error_When_UserId_Is_Empty()
    {
        // Arrange
        var command = new JoinChat.Command(
            Guid.NewGuid(),
            Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Command_Is_Valid()
    {
        // Arrange
        var command = new JoinChat.Command(
            Guid.NewGuid(),
            Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}