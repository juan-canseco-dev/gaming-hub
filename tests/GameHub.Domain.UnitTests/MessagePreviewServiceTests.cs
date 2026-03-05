using GameHub.Domain.Chats;

namespace GameHub.Domain.UnitTests;

public sealed class MessagePreviewServiceTests
{
    [Fact]
    public void CreatePreview_single_line_shorter_than_max_returns_trimmed_and_same_text()
    {
        // Arrange
        var sut = new MessagePreviewService();
        var content = "  hello world  ";

        // Act
        var preview = sut.CreatePreview(content, 200);

        // Assert
        Assert.Equal("hello world", preview);
    }

    [Fact]
    public void CreatePreview_replaces_newlines_tabs_and_carriage_returns_with_spaces_and_trims()
    {
        // Arrange
        var sut = new MessagePreviewService();
        var content = " \r\n hello\tworld \n ";

        // Act
        var preview = sut.CreatePreview(content, 200);

        // Assert
        Assert.Equal("hello world", preview);
    }

    [Fact]
    public void CreatePreview_collapses_multiple_spaces_into_single_spaces()
    {
        // Arrange
        var sut = new MessagePreviewService();
        var content = "hello     world   from   chat";

        // Act
        var preview = sut.CreatePreview(content, 200);

        // Assert
        Assert.Equal("hello world from chat", preview);
    }

    [Fact]
    public void CreatePreview_exactly_max_length_returns_full_string_without_truncation()
    {
        // Arrange
        var sut = new MessagePreviewService();
        var content = new string('a', 200);

        // Act
        var preview = sut.CreatePreview(content, 200);

        // Assert
        Assert.Equal(200, preview.Length);
        Assert.Equal(content, preview);
    }

    [Fact]
    public void CreatePreview_longer_than_max_truncates_to_max_length()
    {
        // Arrange
        var sut = new MessagePreviewService();
        var content = new string('a', 250);

        // Act
        var preview = sut.CreatePreview(content, 200);

        // Assert
        Assert.Equal(200, preview.Length);
        Assert.Equal(new string('a', 200), preview);
    }

    [Fact]
    public void CreatePreview_normalizes_then_truncates_after_normalization()
    {
        // Arrange
        var sut = new MessagePreviewService();
        var content =
            "   hello \n\n\n\t  world   " +
            new string('x', 300);

        // After normalization => "hello world " + 300 x's (single spaces, trimmed)
        // Actually Trim() removes trailing space; so "hello world" + 300 x's with one space between.
        // "hello world " is not kept as trailing due to Trim.
        // Result starts with "hello world " then x's, then truncated.
        var max = 50;

        // Act
        var preview = sut.CreatePreview(content, max);

        // Assert
        Assert.Equal(max, preview.Length);
        Assert.StartsWith("hello world", preview);
    }
}

