using DonkeyWork.Agents.Storage.Core.Services;
using Xunit;

namespace DonkeyWork.Agents.Storage.Tests.Services;

public class FileSystemPathHelperTests
{
    #region IsUserFile Tests

    [Theory]
    [InlineData("readme.txt", true)]
    [InlineData("image.png", true)]
    [InlineData("file with spaces.doc", true)]
    [InlineData("conversations/abc/image.png", false)]
    [InlineData("prefix/file.txt", false)]
    public void IsUserFile_ReturnsExpected(string objectKey, bool expected)
    {
        var result = FileSystemPathHelper.IsUserFile(objectKey);
        Assert.Equal(expected, result);
    }

    #endregion

    #region GetUserDirectory Tests

    [Fact]
    public void GetUserDirectory_BuildsCorrectPath()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var result = FileSystemPathHelper.GetUserDirectory("/mnt/storage", "files", userId);
        Assert.Equal(Path.Combine("/mnt/storage", "files", userId.ToString()), result);
    }

    #endregion

    #region GetSafeFilePath Tests

    [Fact]
    public void GetSafeFilePath_ValidFilename_ReturnsFullPath()
    {
        var userDir = Path.Combine(Path.GetTempPath(), "test-user");
        var result = FileSystemPathHelper.GetSafeFilePath(userDir, "readme.txt");
        Assert.Equal(Path.Combine(userDir, "readme.txt"), result);
    }

    [Fact]
    public void GetSafeFilePath_FilenameWithSpaces_ReturnsFullPath()
    {
        var userDir = Path.Combine(Path.GetTempPath(), "test-user");
        var result = FileSystemPathHelper.GetSafeFilePath(userDir, "my file.txt");
        Assert.Equal(Path.Combine(userDir, "my file.txt"), result);
    }

    [Theory]
    [InlineData("../etc/passwd")]
    [InlineData("..\\etc\\passwd")]
    [InlineData("sub/file.txt")]
    [InlineData("sub\\file.txt")]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSafeFilePath_InvalidFilename_ThrowsArgumentException(string filename)
    {
        var userDir = Path.Combine(Path.GetTempPath(), "test-user");
        Assert.Throws<ArgumentException>(() => FileSystemPathHelper.GetSafeFilePath(userDir, filename));
    }

    #endregion

    #region GetContentType Tests

    [Theory]
    [InlineData("file.txt", "text/plain")]
    [InlineData("image.png", "image/png")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("doc.pdf", "application/pdf")]
    [InlineData("data.json", "application/json")]
    [InlineData("unknown.xyz123", "application/octet-stream")]
    [InlineData("noextension", "application/octet-stream")]
    public void GetContentType_ReturnsExpected(string filename, string expectedContentType)
    {
        var result = FileSystemPathHelper.GetContentType(filename);
        Assert.Equal(expectedContentType, result);
    }

    #endregion
}
