using System.IO.Compression;
using DonkeyWork.Agents.Identity.Contracts.Services;
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
}
