using System.IO.Compression;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Core.Options;
using DonkeyWork.Agents.Storage.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Storage.Tests.Services;

public class SkillsServiceTests : IDisposable
{
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly Mock<ILogger<SkillsService>> _loggerMock;
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly string _tempDir;

    public SkillsServiceTests()
    {
        _identityContextMock = new Mock<IIdentityContext>();
        _identityContextMock.Setup(x => x.UserId).Returns(_userId);
        _loggerMock = new Mock<ILogger<SkillsService>>();
        _tempDir = Path.Combine(Path.GetTempPath(), $"skills-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private SkillsService CreateService(IOptions<StorageOptions>? options = null)
    {
        options ??= Options.Create(new StorageOptions
        {
            ServiceUrl = "http://localhost",
            AccessKey = "test",
            SecretKey = "test",
            FileSystemBasePath = _tempDir,
            SkillsSubPath = "skills"
        });
        return new SkillsService(_identityContextMock.Object, options, _loggerMock.Object);
    }

    private string GetUserSkillsDir()
    {
        return Path.Combine(_tempDir, "skills", _userId.ToString());
    }

    private static MemoryStream CreateZipWithSkill(string skillName, bool includeSkillMd = true, string? skillMdContent = null)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            if (includeSkillMd)
            {
                var entry = archive.CreateEntry($"{skillName}/SKILL.md");
                using var writer = new StreamWriter(entry.Open());
                writer.Write(skillMdContent ?? "# Test Skill");
            }
            else
            {
                var entry = archive.CreateEntry($"{skillName}/readme.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("test");
            }
        }
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateZipWithMultipleFolders()
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var e1 = archive.CreateEntry("skill-a/SKILL.md");
            using (var w1 = new StreamWriter(e1.Open())) w1.Write("# A");
            var e2 = archive.CreateEntry("skill-b/SKILL.md");
            using (var w2 = new StreamWriter(e2.Open())) w2.Write("# B");
        }
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateEmptyZip()
    {
        var ms = new MemoryStream();
        using (var _ = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) { }
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateZipWithMacOSX(string skillName)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var e1 = archive.CreateEntry($"{skillName}/SKILL.md");
            using (var w1 = new StreamWriter(e1.Open())) w1.Write("# Test");
            var e2 = archive.CreateEntry("__MACOSX/._skill");
            using (var w2 = new StreamWriter(e2.Open())) w2.Write("mac junk");
        }
        ms.Position = 0;
        return ms;
    }

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_NoDirectory_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ListAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_EmptyDirectory_ReturnsEmpty()
    {
        // Arrange
        Directory.CreateDirectory(GetUserSkillsDir());
        var service = CreateService();

        // Act
        var result = await service.ListAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_WithSkills_ReturnsSortedItems()
    {
        // Arrange
        var userDir = GetUserSkillsDir();
        Directory.CreateDirectory(Path.Combine(userDir, "beta-skill"));
        Directory.CreateDirectory(Path.Combine(userDir, "alpha-skill"));
        var service = CreateService();

        // Act
        var result = await service.ListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("alpha-skill", result[0].Name);
        Assert.Equal("beta-skill", result[1].Name);
    }

    #endregion

    #region UploadAsync Tests

    [Fact]
    public async Task UploadAsync_ValidZip_CreatesSkillDirectory()
    {
        // Arrange
        var service = CreateService();
        using var zip = CreateZipWithSkill("my-skill");

        // Act
        var result = await service.UploadAsync(zip);

        // Assert
        Assert.Equal("my-skill", result.Name);
        Assert.True(Directory.Exists(Path.Combine(GetUserSkillsDir(), "my-skill")));
        Assert.True(File.Exists(Path.Combine(GetUserSkillsDir(), "my-skill", "SKILL.md")));
    }

    [Fact]
    public async Task UploadAsync_EmptyZip_ThrowsInvalidOperation()
    {
        // Arrange
        var service = CreateService();
        using var zip = CreateEmptyZip();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(zip));
        Assert.Contains("must contain a skill folder", ex.Message);
    }

    [Fact]
    public async Task UploadAsync_MultipleFolders_ThrowsInvalidOperation()
    {
        // Arrange
        var service = CreateService();
        using var zip = CreateZipWithMultipleFolders();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(zip));
        Assert.Contains("exactly one top-level folder", ex.Message);
    }

    [Fact]
    public async Task UploadAsync_MissingSkillMd_ThrowsInvalidOperation()
    {
        // Arrange
        var service = CreateService();
        using var zip = CreateZipWithSkill("my-skill", includeSkillMd: false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(zip));
        Assert.Contains("must contain a SKILL.md file", ex.Message);
    }

    [Fact]
    public async Task UploadAsync_InvalidName_ThrowsInvalidOperation()
    {
        // Arrange
        var service = CreateService();
        using var zip = CreateZipWithSkill("My Skill!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(zip));
        Assert.Contains("Invalid skill name", ex.Message);
    }

    [Fact]
    public async Task UploadAsync_DuplicateSkill_ThrowsInvalidOperation()
    {
        // Arrange
        var service = CreateService();
        Directory.CreateDirectory(Path.Combine(GetUserSkillsDir(), "my-skill"));
        using var zip = CreateZipWithSkill("my-skill");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(zip));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task UploadAsync_MacOSXIgnored_ExtractsCorrectly()
    {
        // Arrange
        var service = CreateService();
        using var zip = CreateZipWithMacOSX("my-skill");

        // Act
        var result = await service.UploadAsync(zip);

        // Assert
        Assert.Equal("my-skill", result.Name);
        Assert.True(Directory.Exists(Path.Combine(GetUserSkillsDir(), "my-skill")));
        Assert.False(Directory.Exists(Path.Combine(GetUserSkillsDir(), "__MACOSX")));
    }

    [Fact]
    public async Task UploadAsync_CleansUpTempOnFailure()
    {
        // Arrange
        var service = CreateService();
        using var zip = CreateEmptyZip();
        var tempDirsBefore = Directory.GetDirectories(Path.GetTempPath(), "skill-upload-*");

        // Act
        try { await service.UploadAsync(zip); } catch { /* expected */ }

        // Assert
        var tempDirsAfter = Directory.GetDirectories(Path.GetTempPath(), "skill-upload-*");
        Assert.Equal(tempDirsBefore.Length, tempDirsAfter.Length);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingSkill_ReturnsTrue()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "# Test");
        var service = CreateService();

        // Act
        var result = await service.DeleteAsync("my-skill");

        // Assert
        Assert.True(result);
        Assert.False(Directory.Exists(skillDir));
    }

    [Fact]
    public async Task DeleteAsync_NonExistentSkill_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DeleteAsync("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_PathTraversal_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.False(await service.DeleteAsync("../etc"));
        Assert.False(await service.DeleteAsync("skill/../../etc"));
        Assert.False(await service.DeleteAsync("skill\\..\\etc"));
    }

    [Fact]
    public async Task DeleteAsync_EmptyName_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DeleteAsync("");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetContentsAsync Tests

    [Fact]
    public async Task GetContentsAsync_ExistingSkill_ReturnsTree()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(Path.Combine(skillDir, "subdir"));
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "# Test");
        File.WriteAllText(Path.Combine(skillDir, "subdir", "nested.txt"), "nested content");
        var service = CreateService();

        // Act
        var result = await service.GetContentsAsync("my-skill");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        // Directory first
        Assert.True(result[0].IsDirectory);
        Assert.Equal("subdir", result[0].Name);
        Assert.NotNull(result[0].Children);
        Assert.Single(result[0].Children);
        Assert.Equal("nested.txt", result[0].Children[0].Name);
        Assert.False(result[0].Children[0].IsDirectory);

        // File second
        Assert.False(result[1].IsDirectory);
        Assert.Equal("SKILL.md", result[1].Name);
        Assert.Null(result[1].Children);
    }

    [Fact]
    public async Task GetContentsAsync_NonExistentSkill_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetContentsAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetContentsAsync_PathTraversal_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Null(await service.GetContentsAsync("../etc"));
        Assert.Null(await service.GetContentsAsync("skill/../../etc"));
        Assert.Null(await service.GetContentsAsync("skill\\..\\etc"));
    }

    [Fact]
    public async Task GetContentsAsync_SortsDirectoriesBeforeFiles()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "zebra.txt"), "z");
        File.WriteAllText(Path.Combine(skillDir, "alpha.txt"), "a");
        Directory.CreateDirectory(Path.Combine(skillDir, "zulu-dir"));
        Directory.CreateDirectory(Path.Combine(skillDir, "alpha-dir"));
        var service = CreateService();

        // Act
        var result = await service.GetContentsAsync("my-skill");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);

        // Directories first, alphabetically
        Assert.True(result[0].IsDirectory);
        Assert.Equal("alpha-dir", result[0].Name);
        Assert.True(result[1].IsDirectory);
        Assert.Equal("zulu-dir", result[1].Name);

        // Files second, alphabetically
        Assert.False(result[2].IsDirectory);
        Assert.Equal("alpha.txt", result[2].Name);
        Assert.False(result[3].IsDirectory);
        Assert.Equal("zebra.txt", result[3].Name);
    }

    #endregion

    #region ReadFileAsync Tests

    [Fact]
    public async Task ReadFileAsync_ExistingFile_ReturnsContent()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "SKILL.md"), "# Hello World");
        var service = CreateService();

        // Act
        var result = await service.ReadFileAsync("my-skill", "SKILL.md");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SKILL.md", result.Name);
        Assert.Equal("SKILL.md", result.Path);
        Assert.Equal("# Hello World", result.Content);
        Assert.Equal("text/markdown", result.ContentType);
        Assert.True(result.Size > 0);
    }

    [Fact]
    public async Task ReadFileAsync_NestedFile_ReturnsContent()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(Path.Combine(skillDir, "subdir"));
        File.WriteAllText(Path.Combine(skillDir, "subdir", "test.txt"), "nested content");
        var service = CreateService();

        // Act
        var result = await service.ReadFileAsync("my-skill", "subdir/test.txt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.txt", result.Name);
        Assert.Equal("subdir/test.txt", result.Path);
        Assert.Equal("nested content", result.Content);
    }

    [Fact]
    public async Task ReadFileAsync_NonExistentFile_ReturnsNull()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act
        var result = await service.ReadFileAsync("my-skill", "nonexistent.txt");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadFileAsync_NonExistentSkill_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.ReadFileAsync("nonexistent", "SKILL.md");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadFileAsync_PathTraversal_ReturnsNull()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act & Assert
        Assert.Null(await service.ReadFileAsync("my-skill", "../../../etc/passwd"));
        Assert.Null(await service.ReadFileAsync("../evil", "SKILL.md"));
    }

    #endregion

    #region WriteFileAsync Tests

    [Fact]
    public async Task WriteFileAsync_NewFile_CreatesFile()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();
        var request = new WriteFileRequestV1 { Content = "new content" };

        // Act
        var result = await service.WriteFileAsync("my-skill", "newfile.txt", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newfile.txt", result.Name);
        Assert.Equal("newfile.txt", result.Path);
        Assert.True(File.Exists(Path.Combine(skillDir, "newfile.txt")));
        Assert.Equal("new content", File.ReadAllText(Path.Combine(skillDir, "newfile.txt")));
    }

    [Fact]
    public async Task WriteFileAsync_ExistingFile_OverwritesContent()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "existing.txt"), "old content");
        var service = CreateService();
        var request = new WriteFileRequestV1 { Content = "updated content" };

        // Act
        var result = await service.WriteFileAsync("my-skill", "existing.txt", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("updated content", File.ReadAllText(Path.Combine(skillDir, "existing.txt")));
    }

    [Fact]
    public async Task WriteFileAsync_NestedPath_CreatesParentDirectories()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();
        var request = new WriteFileRequestV1 { Content = "deep content" };

        // Act
        var result = await service.WriteFileAsync("my-skill", "sub/deep/file.txt", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("file.txt", result.Name);
        Assert.True(File.Exists(Path.Combine(skillDir, "sub", "deep", "file.txt")));
    }

    [Fact]
    public async Task WriteFileAsync_NonExistentSkill_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        var request = new WriteFileRequestV1 { Content = "content" };

        // Act
        var result = await service.WriteFileAsync("nonexistent", "file.txt", request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task WriteFileAsync_PathTraversal_ReturnsNull()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();
        var request = new WriteFileRequestV1 { Content = "evil" };

        // Act
        var result = await service.WriteFileAsync("my-skill", "../../../etc/evil", request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task WriteFileAsync_LargeContent_WritesSuccessfully()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();
        var largeContent = new string('x', 1024 * 1024); // 1MB
        var request = new WriteFileRequestV1 { Content = largeContent };

        // Act
        var result = await service.WriteFileAsync("my-skill", "large.txt", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(largeContent.Length, (int)result.Size);
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_DeletesAndReturnsTrue()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var filePath = Path.Combine(skillDir, "test.txt");
        File.WriteAllText(filePath, "content");
        var service = CreateService();

        // Act
        var result = await service.DeleteFileAsync("my-skill", "test.txt");

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteFileAsync_NonExistentFile_ReturnsFalse()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act
        var result = await service.DeleteFileAsync("my-skill", "nonexistent.txt");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFileAsync_NonExistentSkill_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DeleteFileAsync("nonexistent", "file.txt");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFileAsync_PathTraversal_ReturnsFalse()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act
        Assert.False(await service.DeleteFileAsync("my-skill", "../../../etc/passwd"));
        Assert.False(await service.DeleteFileAsync("../evil", "file.txt"));
    }

    #endregion

    #region RenameAsync Tests

    [Fact]
    public async Task RenameAsync_ExistingFile_RenamesSuccessfully()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "old.txt"), "content");
        var service = CreateService();
        var request = new RenameRequestV1 { NewName = "new.txt" };

        // Act
        var result = await service.RenameAsync("my-skill", "old.txt", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("old.txt", result.OldPath);
        Assert.Equal("new.txt", result.NewPath);
        Assert.Equal("new.txt", result.NewName);
        Assert.False(File.Exists(Path.Combine(skillDir, "old.txt")));
        Assert.True(File.Exists(Path.Combine(skillDir, "new.txt")));
    }

    [Fact]
    public async Task RenameAsync_ExistingFolder_RenamesSuccessfully()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(Path.Combine(skillDir, "old-folder"));
        File.WriteAllText(Path.Combine(skillDir, "old-folder", "file.txt"), "content");
        var service = CreateService();
        var request = new RenameRequestV1 { NewName = "new-folder" };

        // Act
        var result = await service.RenameAsync("my-skill", "old-folder", request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-folder", result.NewName);
        Assert.False(Directory.Exists(Path.Combine(skillDir, "old-folder")));
        Assert.True(Directory.Exists(Path.Combine(skillDir, "new-folder")));
        Assert.True(File.Exists(Path.Combine(skillDir, "new-folder", "file.txt")));
    }

    [Fact]
    public async Task RenameAsync_InvalidNewName_ThrowsInvalidOperation()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "old.txt"), "content");
        var service = CreateService();
        var request = new RenameRequestV1 { NewName = "invalid name!.txt" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RenameAsync("my-skill", "old.txt", request));
        Assert.Contains("Invalid name", ex.Message);
    }

    [Fact]
    public async Task RenameAsync_NameConflict_ThrowsInvalidOperation()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "old.txt"), "old");
        File.WriteAllText(Path.Combine(skillDir, "existing.txt"), "existing");
        var service = CreateService();
        var request = new RenameRequestV1 { NewName = "existing.txt" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RenameAsync("my-skill", "old.txt", request));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task RenameAsync_NonExistentFile_ReturnsNull()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();
        var request = new RenameRequestV1 { NewName = "new.txt" };

        // Act
        var result = await service.RenameAsync("my-skill", "nonexistent.txt", request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RenameAsync_PathTraversal_ReturnsNull()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();
        var request = new RenameRequestV1 { NewName = "new.txt" };

        // Act
        var result = await service.RenameAsync("my-skill", "../../../etc/passwd", request);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DuplicateFileAsync Tests

    [Fact]
    public async Task DuplicateFileAsync_ExistingFile_CreatesCopy()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "test.txt"), "original");
        var service = CreateService();

        // Act
        var result = await service.DuplicateFileAsync("my-skill", "test.txt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-copy.txt", result.Name);
        Assert.Equal("test-copy.txt", result.Path);
        Assert.True(File.Exists(Path.Combine(skillDir, "test-copy.txt")));
        Assert.Equal("original", File.ReadAllText(Path.Combine(skillDir, "test-copy.txt")));
    }

    [Fact]
    public async Task DuplicateFileAsync_CopyAlreadyExists_IncrementsCounter()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "test.txt"), "original");
        File.WriteAllText(Path.Combine(skillDir, "test-copy.txt"), "first copy");
        var service = CreateService();

        // Act
        var result = await service.DuplicateFileAsync("my-skill", "test.txt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-copy-2.txt", result.Name);
        Assert.True(File.Exists(Path.Combine(skillDir, "test-copy-2.txt")));
    }

    [Fact]
    public async Task DuplicateFileAsync_MultipleCopiesExist_FindsNextAvailable()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(Path.Combine(skillDir, "test.txt"), "original");
        File.WriteAllText(Path.Combine(skillDir, "test-copy.txt"), "copy 1");
        File.WriteAllText(Path.Combine(skillDir, "test-copy-2.txt"), "copy 2");
        File.WriteAllText(Path.Combine(skillDir, "test-copy-3.txt"), "copy 3");
        var service = CreateService();

        // Act
        var result = await service.DuplicateFileAsync("my-skill", "test.txt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-copy-4.txt", result.Name);
    }

    [Fact]
    public async Task DuplicateFileAsync_NonExistentFile_ReturnsNull()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act
        var result = await service.DuplicateFileAsync("my-skill", "nonexistent.txt");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DuplicateFileAsync_NonExistentSkill_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DuplicateFileAsync("nonexistent", "file.txt");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DuplicateFileAsync_PathTraversal_ReturnsNull()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act
        var result = await service.DuplicateFileAsync("my-skill", "../../../etc/passwd");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateFolderAsync Tests

    [Fact]
    public async Task CreateFolderAsync_ValidName_CreatesFolder()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act
        var result = await service.CreateFolderAsync("my-skill", "new-folder");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-folder", result.Name);
        Assert.Equal("new-folder", result.Path);
        Assert.True(Directory.Exists(Path.Combine(skillDir, "new-folder")));
    }

    [Fact]
    public async Task CreateFolderAsync_InvalidName_ThrowsInvalidOperation()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateFolderAsync("my-skill", "invalid folder!"));
        Assert.Contains("Invalid folder name", ex.Message);
    }

    [Fact]
    public async Task CreateFolderAsync_AlreadyExists_ThrowsInvalidOperation()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(Path.Combine(skillDir, "existing-folder"));
        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateFolderAsync("my-skill", "existing-folder"));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreateFolderAsync_NonExistentSkill_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CreateFolderAsync("nonexistent", "new-folder");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateFolderAsync_PathTraversal_ReturnsNull()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act
        var result = await service.CreateFolderAsync("my-skill", "../../../etc/evil");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateFolderAsync_NonExistentParent_ReturnsNull()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act
        var result = await service.CreateFolderAsync("my-skill", "nonexistent-parent/new-folder");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteFolderAsync Tests

    [Fact]
    public async Task DeleteFolderAsync_ExistingFolder_DeletesAndReturnsTrue()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        var folderPath = Path.Combine(skillDir, "my-folder");
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(Path.Combine(folderPath, "file.txt"), "content");
        var service = CreateService();

        // Act
        var result = await service.DeleteFolderAsync("my-skill", "my-folder");

        // Assert
        Assert.True(result);
        Assert.False(Directory.Exists(folderPath));
    }

    [Fact]
    public async Task DeleteFolderAsync_NonExistentFolder_ReturnsFalse()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act
        var result = await service.DeleteFolderAsync("my-skill", "nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFolderAsync_NonExistentSkill_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.DeleteFolderAsync("nonexistent", "my-folder");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteFolderAsync_PathTraversal_ReturnsFalse()
    {
        // Arrange
        var skillDir = Path.Combine(GetUserSkillsDir(), "my-skill");
        Directory.CreateDirectory(skillDir);
        var service = CreateService();

        // Act
        Assert.False(await service.DeleteFolderAsync("my-skill", "../../../etc"));
        Assert.False(await service.DeleteFolderAsync("../evil", "folder"));
    }

    #endregion
}
