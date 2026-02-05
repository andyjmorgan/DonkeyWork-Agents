using DonkeyWork.Agents.Conversations.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Conversations.Core.Services;

/// <summary>
/// Service for validating image uploads.
/// </summary>
public class ImageValidationService : IImageValidationService
{
    private readonly ImageValidationOptions _options;
    private readonly ILogger<ImageValidationService> _logger;

    // Magic bytes for image formats
    private static readonly Dictionary<string, byte[][]> MagicBytes = new()
    {
        ["image/jpeg"] = [new byte[] { 0xFF, 0xD8, 0xFF }],
        ["image/png"] = [new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }],
        ["image/gif"] = [new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }],
        ["image/webp"] = [new byte[] { 0x52, 0x49, 0x46, 0x46 }] // RIFF header, followed by WEBP at offset 8
    };

    public ImageValidationService(IOptions<ImageValidationOptions> options, ILogger<ImageValidationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ImageValidationResult> ValidateAsync(string contentType, long fileSizeBytes, Stream fileStream)
    {
        // Validate file size
        if (fileSizeBytes > _options.MaxFileSizeBytes)
        {
            var maxMb = _options.MaxFileSizeBytes / (1024 * 1024);
            return ImageValidationResult.Failure($"File size exceeds maximum allowed size of {maxMb}MB.");
        }

        if (fileSizeBytes == 0)
        {
            return ImageValidationResult.Failure("File is empty.");
        }

        // Validate claimed MIME type
        if (!_options.AllowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return ImageValidationResult.Failure($"Content type '{contentType}' is not allowed. Allowed types: {string.Join(", ", _options.AllowedMimeTypes)}");
        }

        // Validate magic bytes
        var detectedMimeType = await DetectMimeTypeFromMagicBytesAsync(fileStream);

        if (detectedMimeType == null)
        {
            return ImageValidationResult.Failure("Could not verify file type from content. File may be corrupted or not a valid image.");
        }

        if (!_options.AllowedMimeTypes.Contains(detectedMimeType, StringComparer.OrdinalIgnoreCase))
        {
            return ImageValidationResult.Failure($"Detected file type '{detectedMimeType}' is not allowed.");
        }

        // Reset stream position for subsequent reads
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        _logger.LogDebug("Image validation passed. Claimed type: {ClaimedType}, Detected type: {DetectedType}, Size: {Size} bytes",
            contentType, detectedMimeType, fileSizeBytes);

        return ImageValidationResult.Success(detectedMimeType);
    }

    private async Task<string?> DetectMimeTypeFromMagicBytesAsync(Stream stream)
    {
        // Read enough bytes to check all magic byte patterns
        var buffer = new byte[12];
        var originalPosition = stream.CanSeek ? stream.Position : 0;

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        var bytesRead = await stream.ReadAsync(buffer);

        if (bytesRead < 4)
        {
            return null;
        }

        // Check each format
        foreach (var (mimeType, patterns) in MagicBytes)
        {
            foreach (var pattern in patterns)
            {
                if (bytesRead >= pattern.Length && buffer.AsSpan(0, pattern.Length).SequenceEqual(pattern))
                {
                    // Special check for WebP: verify WEBP signature at offset 8
                    if (mimeType == "image/webp")
                    {
                        if (bytesRead >= 12 && buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                        {
                            return mimeType;
                        }
                        continue; // Not a valid WebP
                    }

                    return mimeType;
                }
            }
        }

        // Reset stream position
        if (stream.CanSeek)
        {
            stream.Position = originalPosition;
        }

        return null;
    }
}

/// <summary>
/// Options for image validation.
/// </summary>
public class ImageValidationOptions
{
    /// <summary>
    /// Maximum file size in bytes.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed MIME types.
    /// </summary>
    public string[] AllowedMimeTypes { get; set; } = ["image/jpeg", "image/png", "image/gif", "image/webp"];
}
