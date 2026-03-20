using System.IO.Compression;
using System.Text.RegularExpressions;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using DonkeyWork.Agents.Storage.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Storage.Core.Services;

public sealed partial class SkillsService : ISkillsService
{
    private readonly IIdentityContext _identityContext;
    private readonly StorageOptions _options;
    private readonly ILogger<SkillsService> _logger;

    [GeneratedRegex("^[a-z0-9][a-z0-9\\-_]*$")]
    private static partial Regex SkillNameRegex();

    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9._\\-]*$")]
    private static partial Regex FileNameRegex();

    public SkillsService(
        IIdentityContext identityContext,
        IOptions<StorageOptions> options,
        ILogger<SkillsService> logger)
    {
        _identityContext = identityContext;
        _options = options.Value;
        _logger = logger;
    }

    private string GetUserSkillsDirectory()
    {
        if (string.IsNullOrEmpty(_options.FileSystemBasePath))
            throw new InvalidOperationException("FileSystemBasePath is not configured.");

        return FileSystemPathHelper.GetUserDirectory(
            _options.FileSystemBasePath, _options.SkillsSubPath, _identityContext.UserId);
    }

    public Task<IReadOnlyList<SkillItemV1>> ListAsync(CancellationToken ct = default)
    {
        var userDir = GetUserSkillsDirectory();

        if (!Directory.Exists(userDir))
            return Task.FromResult<IReadOnlyList<SkillItemV1>>(Array.Empty<SkillItemV1>());

        var items = Directory.GetDirectories(userDir)
            .Select(dir => new SkillItemV1
            {
                Name = Path.GetFileName(dir),
                CreatedAt = new DateTimeOffset(Directory.GetCreationTimeUtc(dir), TimeSpan.Zero)
            })
            .OrderBy(s => s.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<SkillItemV1>>(items);
    }

    public async Task<SkillUploadResultV1> UploadAsync(Stream zipStream, CancellationToken ct = default)
    {
        var userDir = GetUserSkillsDirectory();
        var tempDir = Path.Combine(Path.GetTempPath(), $"skill-upload-{Guid.NewGuid()}");

        try
        {
            Directory.CreateDirectory(tempDir);

            var tempZipPath = Path.Combine(tempDir, "upload.zip");
            await using (var fileStream = File.Create(tempZipPath))
            {
                await zipStream.CopyToAsync(fileStream, ct);
            }

            ZipFile.ExtractToDirectory(tempZipPath, tempDir);
            File.Delete(tempZipPath);

            var topLevelDirs = Directory.GetDirectories(tempDir)
                .Select(d => new DirectoryInfo(d))
                .Where(d => d.Name != "__MACOSX" && !d.Name.StartsWith('.'))
                .ToList();

            if (topLevelDirs.Count == 0)
                throw new InvalidOperationException("Zip archive must contain a skill folder.");

            if (topLevelDirs.Count > 1)
                throw new InvalidOperationException("Zip archive must contain exactly one top-level folder.");

            var skillDir = topLevelDirs[0];
            var skillName = skillDir.Name;

            if (!SkillNameRegex().IsMatch(skillName))
                throw new InvalidOperationException(
                    $"Invalid skill name '{skillName}'. Must match pattern: lowercase letters, numbers, hyphens, underscores.");

            if (!File.Exists(Path.Combine(skillDir.FullName, "SKILL.md")))
                throw new InvalidOperationException($"Skill folder '{skillName}' must contain a SKILL.md file.");

            Directory.CreateDirectory(userDir);

            var destDir = Path.Combine(userDir, skillName);
            var canonicalUserDir = Path.GetFullPath(userDir + Path.DirectorySeparatorChar);
            var canonicalDestDir = Path.GetFullPath(destDir + Path.DirectorySeparatorChar);

            if (!canonicalDestDir.StartsWith(canonicalUserDir, StringComparison.Ordinal))
                throw new InvalidOperationException("Skill name resolves outside the user directory.");

            if (Directory.Exists(destDir))
                throw new InvalidOperationException($"Skill '{skillName}' already exists.");

            CopyDirectory(skillDir.FullName, destDir);

            _logger.LogInformation("Skill '{SkillName}' uploaded for user {UserId}", skillName, _identityContext.UserId);

            return new SkillUploadResultV1 { Name = skillName };
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); }
                catch { /* best effort cleanup */ }
            }
        }
    }

    public Task<bool> DeleteAsync(string skillName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(skillName) ||
            skillName.Contains('/') || skillName.Contains('\\') || skillName.Contains(".."))
            return Task.FromResult(false);

        var userDir = GetUserSkillsDirectory();
        var skillDir = Path.Combine(userDir, skillName);
        var canonicalUserDir = Path.GetFullPath(userDir + Path.DirectorySeparatorChar);
        var canonicalSkillDir = Path.GetFullPath(skillDir + Path.DirectorySeparatorChar);

        if (!canonicalSkillDir.StartsWith(canonicalUserDir, StringComparison.Ordinal))
            return Task.FromResult(false);

        if (!Directory.Exists(skillDir))
            return Task.FromResult(false);

        Directory.Delete(skillDir, true);

        _logger.LogInformation("Skill '{SkillName}' deleted for user {UserId}", skillName, _identityContext.UserId);

        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<SkillFileNodeV1>?> GetContentsAsync(string skillName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(skillName) ||
            skillName.Contains('/') || skillName.Contains('\\') || skillName.Contains(".."))
            return Task.FromResult<IReadOnlyList<SkillFileNodeV1>?>(null);

        var userDir = GetUserSkillsDirectory();
        var skillDir = Path.Combine(userDir, skillName);
        var canonicalUserDir = Path.GetFullPath(userDir + Path.DirectorySeparatorChar);
        var canonicalSkillDir = Path.GetFullPath(skillDir + Path.DirectorySeparatorChar);

        if (!canonicalSkillDir.StartsWith(canonicalUserDir, StringComparison.Ordinal))
            return Task.FromResult<IReadOnlyList<SkillFileNodeV1>?>(null);

        if (!Directory.Exists(skillDir))
            return Task.FromResult<IReadOnlyList<SkillFileNodeV1>?>(null);

        var tree = BuildTree(new DirectoryInfo(skillDir));
        return Task.FromResult<IReadOnlyList<SkillFileNodeV1>?>(tree);
    }

    /// <summary>
    /// Validates the skill name and relative path, returning the absolute path if valid, or null if invalid.
    /// </summary>
    private string? ResolveAndValidatePath(string skillName, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(skillName) ||
            skillName.Contains('/') || skillName.Contains('\\') || skillName.Contains(".."))
            return null;

        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var userDir = GetUserSkillsDirectory();
        var skillDir = Path.Combine(userDir, skillName);
        var canonicalUserDir = Path.GetFullPath(userDir + Path.DirectorySeparatorChar);
        var canonicalSkillDir = Path.GetFullPath(skillDir + Path.DirectorySeparatorChar);

        if (!canonicalSkillDir.StartsWith(canonicalUserDir, StringComparison.Ordinal))
            return null;

        if (!Directory.Exists(skillDir))
            return null;

        var targetPath = Path.GetFullPath(Path.Combine(skillDir, relativePath));

        if (!targetPath.StartsWith(canonicalSkillDir, StringComparison.Ordinal))
            return null;

        return targetPath;
    }

    public Task<ReadFileResponseV1?> ReadFileAsync(string skillName, string relativePath, CancellationToken ct = default)
    {
        var targetPath = ResolveAndValidatePath(skillName, relativePath);
        if (targetPath is null || !File.Exists(targetPath))
            return Task.FromResult<ReadFileResponseV1?>(null);

        var fileInfo = new FileInfo(targetPath);
        var content = File.ReadAllText(targetPath);
        var contentType = FileSystemPathHelper.GetContentType(fileInfo.Name);

        _logger.LogInformation("Read file '{Path}' in skill '{SkillName}' for user {UserId}",
            relativePath, skillName, _identityContext.UserId);

        return Task.FromResult<ReadFileResponseV1?>(new ReadFileResponseV1
        {
            Path = relativePath,
            Name = fileInfo.Name,
            Content = content,
            ContentType = contentType,
            Size = fileInfo.Length,
            LastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero)
        });
    }

    public Task<WriteFileResponseV1?> WriteFileAsync(string skillName, string relativePath, WriteFileRequestV1 request, CancellationToken ct = default)
    {
        var targetPath = ResolveAndValidatePath(skillName, relativePath);
        if (targetPath is null)
            return Task.FromResult<WriteFileResponseV1?>(null);

        var parentDir = Path.GetDirectoryName(targetPath);
        if (parentDir is not null && !Directory.Exists(parentDir))
            Directory.CreateDirectory(parentDir);

        File.WriteAllText(targetPath, request.Content);

        var fileInfo = new FileInfo(targetPath);

        _logger.LogInformation("Wrote file '{Path}' in skill '{SkillName}' for user {UserId}",
            relativePath, skillName, _identityContext.UserId);

        return Task.FromResult<WriteFileResponseV1?>(new WriteFileResponseV1
        {
            Path = relativePath,
            Name = fileInfo.Name,
            Size = fileInfo.Length,
            LastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero)
        });
    }

    public Task<bool> DeleteFileAsync(string skillName, string relativePath, CancellationToken ct = default)
    {
        var targetPath = ResolveAndValidatePath(skillName, relativePath);
        if (targetPath is null || !File.Exists(targetPath))
            return Task.FromResult(false);

        File.Delete(targetPath);

        _logger.LogInformation("Deleted file '{Path}' in skill '{SkillName}' for user {UserId}",
            relativePath, skillName, _identityContext.UserId);

        return Task.FromResult(true);
    }

    public Task<RenameResponseV1?> RenameAsync(string skillName, string relativePath, RenameRequestV1 request, CancellationToken ct = default)
    {
        var targetPath = ResolveAndValidatePath(skillName, relativePath);
        if (targetPath is null)
            return Task.FromResult<RenameResponseV1?>(null);

        if (!File.Exists(targetPath) && !Directory.Exists(targetPath))
            return Task.FromResult<RenameResponseV1?>(null);

        if (!FileNameRegex().IsMatch(request.NewName))
            throw new InvalidOperationException(
                $"Invalid name '{request.NewName}'. Must start with alphanumeric and contain only letters, numbers, dots, hyphens, or underscores.");

        var parentDir = Path.GetDirectoryName(targetPath)!;
        var newPath = Path.Combine(parentDir, request.NewName);

        if (File.Exists(newPath) || Directory.Exists(newPath))
            throw new InvalidOperationException($"An item named '{request.NewName}' already exists at this location.");

        var isDirectory = Directory.Exists(targetPath);
        if (isDirectory)
            Directory.Move(targetPath, newPath);
        else
            File.Move(targetPath, newPath);

        var oldRelativePath = relativePath;
        var newRelativePath = Path.Combine(Path.GetDirectoryName(relativePath) ?? string.Empty, request.NewName)
            .Replace('\\', '/');

        _logger.LogInformation("Renamed '{OldPath}' to '{NewPath}' in skill '{SkillName}' for user {UserId}",
            oldRelativePath, newRelativePath, skillName, _identityContext.UserId);

        return Task.FromResult<RenameResponseV1?>(new RenameResponseV1
        {
            OldPath = oldRelativePath,
            NewPath = newRelativePath,
            NewName = request.NewName
        });
    }

    public Task<DuplicateFileResponseV1?> DuplicateFileAsync(string skillName, string relativePath, CancellationToken ct = default)
    {
        var targetPath = ResolveAndValidatePath(skillName, relativePath);
        if (targetPath is null || !File.Exists(targetPath))
            return Task.FromResult<DuplicateFileResponseV1?>(null);

        var directory = Path.GetDirectoryName(targetPath)!;
        var fileName = Path.GetFileNameWithoutExtension(targetPath);
        var extension = Path.GetExtension(targetPath);

        var copyName = $"{fileName}-copy{extension}";
        var copyPath = Path.Combine(directory, copyName);

        if (File.Exists(copyPath))
        {
            var counter = 2;
            do
            {
                copyName = $"{fileName}-copy-{counter}{extension}";
                copyPath = Path.Combine(directory, copyName);
                counter++;
            } while (File.Exists(copyPath));
        }

        File.Copy(targetPath, copyPath);

        var relativeDir = Path.GetDirectoryName(relativePath) ?? string.Empty;
        var newRelativePath = Path.Combine(relativeDir, copyName).Replace('\\', '/');

        _logger.LogInformation("Duplicated file '{Path}' as '{NewPath}' in skill '{SkillName}' for user {UserId}",
            relativePath, newRelativePath, skillName, _identityContext.UserId);

        return Task.FromResult<DuplicateFileResponseV1?>(new DuplicateFileResponseV1
        {
            Path = newRelativePath,
            Name = copyName
        });
    }

    public Task<CreateFolderResponseV1?> CreateFolderAsync(string skillName, string relativePath, CancellationToken ct = default)
    {
        var targetPath = ResolveAndValidatePath(skillName, relativePath);
        if (targetPath is null)
            return Task.FromResult<CreateFolderResponseV1?>(null);

        var folderName = Path.GetFileName(targetPath);
        if (!FileNameRegex().IsMatch(folderName))
            throw new InvalidOperationException(
                $"Invalid folder name '{folderName}'. Must start with alphanumeric and contain only letters, numbers, dots, hyphens, or underscores.");

        if (Directory.Exists(targetPath) || File.Exists(targetPath))
            throw new InvalidOperationException($"An item named '{folderName}' already exists at this location.");

        var parentDir = Path.GetDirectoryName(targetPath);
        if (parentDir is not null && !Directory.Exists(parentDir))
            return Task.FromResult<CreateFolderResponseV1?>(null);

        Directory.CreateDirectory(targetPath);

        _logger.LogInformation("Created folder '{Path}' in skill '{SkillName}' for user {UserId}",
            relativePath, skillName, _identityContext.UserId);

        return Task.FromResult<CreateFolderResponseV1?>(new CreateFolderResponseV1
        {
            Path = relativePath,
            Name = folderName
        });
    }

    public Task<bool> DeleteFolderAsync(string skillName, string relativePath, CancellationToken ct = default)
    {
        var targetPath = ResolveAndValidatePath(skillName, relativePath);
        if (targetPath is null || !Directory.Exists(targetPath))
            return Task.FromResult(false);

        Directory.Delete(targetPath, true);

        _logger.LogInformation("Deleted folder '{Path}' in skill '{SkillName}' for user {UserId}",
            relativePath, skillName, _identityContext.UserId);

        return Task.FromResult(true);
    }

    public Task<Stream?> DownloadAsync(string skillName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(skillName) ||
            skillName.Contains('/') || skillName.Contains('\\') || skillName.Contains(".."))
            return Task.FromResult<Stream?>(null);

        var userDir = GetUserSkillsDirectory();
        var skillDir = Path.Combine(userDir, skillName);
        var canonicalUserDir = Path.GetFullPath(userDir + Path.DirectorySeparatorChar);
        var canonicalSkillDir = Path.GetFullPath(skillDir + Path.DirectorySeparatorChar);

        if (!canonicalSkillDir.StartsWith(canonicalUserDir, StringComparison.Ordinal))
            return Task.FromResult<Stream?>(null);

        if (!Directory.Exists(skillDir))
            return Task.FromResult<Stream?>(null);

        var memoryStream = new MemoryStream();
        ZipFile.CreateFromDirectory(skillDir, memoryStream, CompressionLevel.Optimal, includeBaseDirectory: true);
        memoryStream.Position = 0;

        _logger.LogInformation("Downloaded skill '{SkillName}' for user {UserId}", skillName, _identityContext.UserId);

        return Task.FromResult<Stream?>(memoryStream);
    }

    private static IReadOnlyList<SkillFileNodeV1> BuildTree(DirectoryInfo dir)
    {
        var nodes = new List<SkillFileNodeV1>();

        foreach (var subDir in dir.GetDirectories().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
        {
            nodes.Add(new SkillFileNodeV1
            {
                Name = subDir.Name,
                IsDirectory = true,
                Children = BuildTree(subDir)
            });
        }

        foreach (var file in dir.GetFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
        {
            nodes.Add(new SkillFileNodeV1
            {
                Name = file.Name,
                IsDirectory = false,
                Children = null
            });
        }

        return nodes;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }
}
