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
}
