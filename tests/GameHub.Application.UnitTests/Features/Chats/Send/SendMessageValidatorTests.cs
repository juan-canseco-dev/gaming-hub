using GameHub.Domain.Chats;
using GameHub.Application.Features.Chats.SendMessage;
using FluentValidation.TestHelper;

namespace GameHub.Application.UnitTests.Features.Chats.Send;

public sealed class SendMessageValidatorTests
{
    private readonly ChatSendMessage.Validator _validator;

    public SendMessageValidatorTests()
    {
        _validator = new ChatSendMessage.Validator();
    }

    [Fact]
    public void Should_Have_Error_When_ChatId_Is_Empty()
    {
        // Arrange
        var command = new ChatSendMessage.Command(
            Guid.Empty,
            Guid.NewGuid(),
            "hello");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChatId);
    }

    [Fact]
    public void Should_Have_Error_When_UserId_Is_Empty()
    {
        // Arrange
        var command = new ChatSendMessage.Command(
            Guid.NewGuid(),
            Guid.Empty,
            "hello");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_Have_Error_When_Content_Is_Empty()
    {
        // Arrange
        var command = new ChatSendMessage.Command(
            Guid.NewGuid(),
            Guid.NewGuid(),
            string.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_Have_Error_When_Content_Exceeds_Max_Length()
    {
        // Arrange
        var content = new string('a', Chat.MaxMessageLength + 1);

        var command = new ChatSendMessage.Command(
            Guid.NewGuid(),
            Guid.NewGuid(),
            content);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Command_Is_Valid()
    {
        // Arrange
        var command = new ChatSendMessage.Command(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Hello world");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}