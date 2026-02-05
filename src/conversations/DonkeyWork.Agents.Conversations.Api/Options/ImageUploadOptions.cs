using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Conversations.Api.Options;

/// <summary>
/// Configuration options for image uploads in conversations.
/// </summary>
public class ImageUploadOptions
{
    /// <summary>
    /// Maximum file size in bytes. Default is 10MB.
    /// </summary>
    [Range(1, long.MaxValue)]
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed MIME types for image uploads.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string[] AllowedMimeTypes { get; set; } = ["image/jpeg", "image/png", "image/gif", "image/webp"];
}
