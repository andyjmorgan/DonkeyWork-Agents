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

    public TimeSpan DefaultShareExpiry { get; set; } = TimeSpan.FromDays(1);

    public TimeSpan FileDeletionGracePeriod { get; set; } = TimeSpan.FromDays(30);

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
}
