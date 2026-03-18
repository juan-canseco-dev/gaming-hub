using FluentValidation.TestHelper;
using GameHub.Application.Features.Chats.Commands.Join;

namespace GameHub.Application.UnitTests.Features.Chats.Commands.Join;

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
            Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChatId);
    }

   

    [Fact]
    public void Should_Not_Have_Errors_When_Command_Is_Valid()
    {
        // Arrange
        var command = new JoinChat.Command(
            Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}