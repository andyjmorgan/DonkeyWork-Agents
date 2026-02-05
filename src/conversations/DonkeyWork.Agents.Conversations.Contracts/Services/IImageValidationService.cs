namespace DonkeyWork.Agents.Conversations.Contracts.Services;

/// <summary>
/// Service for validating image uploads.
/// </summary>
public interface IImageValidationService
{
    /// <summary>
    /// Validates an image file for upload.
    /// </summary>
    /// <param name="contentType">The MIME type of the file.</param>
    /// <param name="fileSizeBytes">The size of the file in bytes.</param>
    /// <param name="fileStream">The file stream to validate magic bytes.</param>
    /// <returns>A validation result with success status and error message if failed.</returns>
    Task<ImageValidationResult> ValidateAsync(string contentType, long fileSizeBytes, Stream fileStream);
}

/// <summary>
/// Result of image validation.
/// </summary>
public sealed class ImageValidationResult
{
    /// <summary>
    /// Whether the image is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The detected MIME type from magic bytes (may differ from claimed type).
    /// </summary>
    public string? DetectedMimeType { get; init; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ImageValidationResult Success(string detectedMimeType) => new()
    {
        IsValid = true,
        DetectedMimeType = detectedMimeType
    };

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ImageValidationResult Failure(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}
