using GameHub.Domain.Chats;
using GameHub.Domain.Channels;

namespace GameHub.Domain.UnitTests;

public class MessageTests
{
    private readonly MessagePreviewService previewService = new();

    [Fact]
    public void AddMessage_valid_input_adds_message_and_updates_last_message_state()
    {
        // Arrange
        var chat = Chat.Create(1, new DateTime(2026, 03, 03, 10, 30, 00, DateTimeKind.Utc)).Value;

        var senderUserId = Guid.NewGuid();
        var content = "hello!";
        var createdAt = new DateTimeOffset(2026, 03, 03, 10, 31, 00, TimeSpan.Zero);

        // Act
        var result = chat.AddMessage(senderUserId, content, createdAt, previewService);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);

        var message = result.Value;
        Assert.NotNull(message);
        Assert.Equal(ChatMessageType.User, message.Type);

        Assert.Single(chat.Messages);
        Assert.Equal(createdAt, chat.LastMessageAt);
        Assert.Equal(message.Id, chat.LastMessageId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddMessage_null_or_empty_content_returns_failure(string? invalidContent)
    {
        // Arrange
        var chat = Chat.Create(1, new DateTime(2026, 03, 03, 10, 30, 00, DateTimeKind.Utc)).Value;

        var senderUserId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 03, 03, 10, 31, 00, TimeSpan.Zero);
        var expectedError = MessageErrors.MessageContentRequired();

        // Act
        var result = chat.AddMessage(senderUserId, invalidContent!, createdAt, previewService);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);

        Assert.Equal(expectedError.Code, result.Error.Code);
        Assert.Equal(expectedError.Description, result.Error.Description);

        Assert.Empty(chat.Messages);
        Assert.Equal(default, chat.LastMessageAt);
        Assert.Equal(default, chat.LastMessageId);
    }

    [Fact]
    public void AddMessage_content_too_long_returns_failure_and_does_not_mutate_state()
    {
        // Arrange
        var chat = Chat.Create(1, new DateTime(2026, 03, 03, 10, 30, 00, DateTimeKind.Utc)).Value;

        var senderUserId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 03, 03, 10, 31, 00, TimeSpan.Zero);

        var tooLong = new string('a', Chat.MaxMessageLength + 1);
        var expectedError = MessageErrors.MessageTooLong(Chat.MaxMessageLength);

        // Act
        var result = chat.AddMessage(senderUserId, tooLong, createdAt, previewService);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);

        Assert.Equal(expectedError.Code, result.Error.Code);
        Assert.Equal(expectedError.Description, result.Error.Description);

        Assert.Empty(chat.Messages);
        Assert.Equal(default, chat.LastMessageAt);
        Assert.Equal(default, chat.LastMessageId);
    }

    [Fact]
    public void AddMessage_valid_input_sets_LastMessagePreview_using_service_and_max_preview_length()
    {
        // Arrange
        var chat = Chat.Create(1, new DateTime(2026, 03, 03, 10, 30, 00, DateTimeKind.Utc)).Value;

        var senderUserId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 03, 03, 10, 31, 00, TimeSpan.Zero);

        var content = " \n hello\t\tworld   from   chat \r\n ";

        // Act
        var result = chat.AddMessage(senderUserId, content, createdAt, previewService);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("hello world from chat", chat.LastMessagePreview);
    }

    [Fact]
    public void AddMessage_when_content_exceeds_preview_max_sets_truncated_LastMessagePreview()
    {
        // Arrange
        var chat = Chat.Create(1, new DateTime(2026, 03, 03, 10, 30, 00, DateTimeKind.Utc)).Value;

        var senderUserId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 03, 03, 10, 31, 00, TimeSpan.Zero);

        var content = new string('a', Chat.MaxPreviewLength + 20);

        // Act
        var result = chat.AddMessage(senderUserId, content, createdAt, previewService);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(chat.LastMessagePreview);
        Assert.Equal(Chat.MaxPreviewLength, chat.LastMessagePreview!.Length);
        Assert.Equal(new string('a', Chat.MaxPreviewLength), chat.LastMessagePreview);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AddMessage_invalid_content_does_not_set_LastMessagePreview(string? invalidContent)
    {
        // Arrange
        var chat = Chat.Create(1, new DateTime(2026, 03, 03, 10, 30, 00, DateTimeKind.Utc)).Value;

        var senderUserId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 03, 03, 10, 31, 00, TimeSpan.Zero);

        // Act
        var result = chat.AddMessage(senderUserId, invalidContent!, createdAt, previewService);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Null(chat.LastMessagePreview);
        Assert.Empty(chat.Messages);
    }

    [Fact]
    public void AddMessage_too_long_message_does_not_set_LastMessagePreview()
    {
        // Arrange
        var chat = Chat.Create(1, new DateTime(2026, 03, 03, 10, 30, 00, DateTimeKind.Utc)).Value;

        var senderUserId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 03, 03, 10, 31, 00, TimeSpan.Zero);

        var content = new string('a', Chat.MaxMessageLength + 1);

        // Act
        var result = chat.AddMessage(senderUserId, content, createdAt, previewService);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Null(chat.LastMessagePreview);
        Assert.Empty(chat.Messages);
        Assert.Equal(default, chat.LastMessageAt);
        Assert.Equal(default, chat.LastMessageId);
    }
}
