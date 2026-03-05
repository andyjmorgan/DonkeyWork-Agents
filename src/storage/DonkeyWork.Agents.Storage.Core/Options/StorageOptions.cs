using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Storage.Core.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string ServiceUrl { get; set; } = string.Empty;

    [Required]
    public string AccessKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string DefaultBucket { get; set; } = "files";

    public bool UsePathStyleAddressing { get; set; } = true;

    /// <summary>
    /// Public-facing URL for generating presigned URLs.
    /// If not set, ServiceUrl will be used.
    /// </summary>
    public string? PublicServiceUrl { get; set; }

    /// <summary>
    /// Default expiry for presigned URLs.
    /// </summary>
    public TimeSpan DefaultPresignedUrlExpiry { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Base path for filesystem-backed user file storage (e.g., "/mnt/storage").
    /// When set, user files (keys without '/') are stored on the filesystem instead of S3.
    /// When null, all storage uses S3 (backwards compatible).
    /// </summary>
    public string? FileSystemBasePath { get; set; }

    /// <summary>
    /// Sub-path within <see cref="FileSystemBasePath"/> for user files.
    /// Full path: {FileSystemBasePath}/{UserFilesSubPath}/{userId}/
    /// </summary>
    public string UserFilesSubPath { get; set; } = "files";

    /// <summary>
    /// Sub-path within <see cref="FileSystemBasePath"/> for per-user skills.
    /// Full path: {FileSystemBasePath}/{SkillsSubPath}/{userId}/
    /// </summary>
    public string SkillsSubPath { get; set; } = "skills";
}
