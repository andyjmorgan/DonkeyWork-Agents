using DonkeyWork.Agents.Projects.Contracts.Helpers;

namespace DonkeyWork.Agents.Projects.Core.Tests.Helpers;

/// <summary>
/// Unit tests for ContentTruncationHelper.
/// </summary>
public class ContentTruncationHelperTests
{
    #region TruncateContent Tests

    [Fact]
    public void TruncateContent_NullContent_ReturnsNull()
    {
        // Act
        var result = ContentTruncationHelper.TruncateContent(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TruncateContent_EmptyContent_ReturnsEmpty()
    {
        // Act
        var result = ContentTruncationHelper.TruncateContent("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void TruncateContent_ShortContent_ReturnsUnchanged()
    {
        // Arrange
        var content = "Short content";

        // Act
        var result = ContentTruncationHelper.TruncateContent(content);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void TruncateContent_ExactLengthContent_ReturnsUnchanged()
    {
        // Arrange
        var content = new string('a', ContentTruncationHelper.DefaultPreviewLength);

        // Act
        var result = ContentTruncationHelper.TruncateContent(content);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void TruncateContent_LongContent_TruncatesWithEllipsis()
    {
        // Arrange
        var content = new string('a', ContentTruncationHelper.DefaultPreviewLength + 100);

        // Act
        var result = ContentTruncationHelper.TruncateContent(content);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ContentTruncationHelper.DefaultPreviewLength + 3, result.Length); // 500 + "..."
        Assert.EndsWith("...", result);
        Assert.StartsWith(new string('a', ContentTruncationHelper.DefaultPreviewLength), result);
    }

    [Fact]
    public void TruncateContent_CustomMaxLength_TruncatesCorrectly()
    {
        // Arrange
        var content = "Hello World! This is a test.";

        // Act
        var result = ContentTruncationHelper.TruncateContent(content, 5);

        // Assert
        Assert.Equal("Hello...", result);
    }

    [Fact]
    public void TruncateContent_ContentOneLongerThanMax_Truncates()
    {
        // Arrange
        var content = new string('x', ContentTruncationHelper.DefaultPreviewLength + 1);

        // Act
        var result = ContentTruncationHelper.TruncateContent(content);

        // Assert
        Assert.NotNull(result);
        Assert.EndsWith("...", result);
        Assert.Equal(ContentTruncationHelper.DefaultPreviewLength + 3, result.Length);
    }

    #endregion

    #region GetContentLength Tests

    [Fact]
    public void GetContentLength_NullContent_ReturnsZero()
    {
        // Act
        var result = ContentTruncationHelper.GetContentLength(null);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetContentLength_EmptyContent_ReturnsZero()
    {
        // Act
        var result = ContentTruncationHelper.GetContentLength("");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetContentLength_NonEmptyContent_ReturnsCorrectLength()
    {
        // Arrange
        var content = "Hello World!";

        // Act
        var result = ContentTruncationHelper.GetContentLength(content);

        // Assert
        Assert.Equal(12, result);
    }

    [Fact]
    public void GetContentLength_LargeContent_ReturnsCorrectLength()
    {
        // Arrange
        var content = new string('a', 10000);

        // Act
        var result = ContentTruncationHelper.GetContentLength(content);

        // Assert
        Assert.Equal(10000, result);
    }

    #endregion

    #region ApplyChunking Tests

    [Fact]
    public void ApplyChunking_NullContent_ReturnsNull()
    {
        // Act
        var result = ContentTruncationHelper.ApplyChunking(null, 0, 10);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ApplyChunking_NullOffsetAndLength_ReturnsFullContent()
    {
        // Arrange
        var content = "Hello World!";

        // Act
        var result = ContentTruncationHelper.ApplyChunking(content, null, null);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void ApplyChunking_WithOffset_ReturnsFromOffset()
    {
        // Arrange
        var content = "Hello World!";

        // Act
        var result = ContentTruncationHelper.ApplyChunking(content, 6, null);

        // Assert
        Assert.Equal("World!", result);
    }

    [Fact]
    public void ApplyChunking_WithLength_ReturnsLimitedChars()
    {
        // Arrange
        var content = "Hello World!";

        // Act
        var result = ContentTruncationHelper.ApplyChunking(content, null, 5);

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ApplyChunking_WithOffsetAndLength_ReturnsChunk()
    {
        // Arrange
        var content = "Hello World!";

        // Act
        var result = ContentTruncationHelper.ApplyChunking(content, 6, 5);

        // Assert
        Assert.Equal("World", result);
    }

    [Fact]
    public void ApplyChunking_OffsetBeyondLength_ReturnsEmpty()
    {
        // Arrange
        var content = "Hello";

        // Act
        var result = ContentTruncationHelper.ApplyChunking(content, 100, 10);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ApplyChunking_LengthExceedsRemaining_ReturnsRemaining()
    {
        // Arrange
        var content = "Hello World!";

        // Act
        var result = ContentTruncationHelper.ApplyChunking(content, 6, 100);

        // Assert
        Assert.Equal("World!", result);
    }

    [Fact]
    public void ApplyChunking_ZeroOffset_ReturnsFromStart()
    {
        // Arrange
        var content = "Hello World!";

        // Act
        var result = ContentTruncationHelper.ApplyChunking(content, 0, 5);

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ApplyChunking_ZeroLength_ReturnsEmpty()
    {
        // Arrange
        var content = "Hello World!";

        // Act
        var result = ContentTruncationHelper.ApplyChunking(content, 0, 0);

        // Assert
        Assert.Equal("", result);
    }

    #endregion
}
