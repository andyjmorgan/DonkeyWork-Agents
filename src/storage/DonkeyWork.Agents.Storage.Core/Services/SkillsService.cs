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
