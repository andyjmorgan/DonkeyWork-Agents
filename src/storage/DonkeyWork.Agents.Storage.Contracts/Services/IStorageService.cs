using DonkeyWork.Agents.Storage.Contracts.Models;

namespace DonkeyWork.Agents.Storage.Contracts.Services;

public interface IStorageService
{
    /// <summary>
    /// Gets a presigned URL for direct access to a file.
    /// </summary>
    /// <param name="id">The file ID.</param>
    /// <param name="expiry">Optional expiry duration. Uses default if not specified.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Presigned URL result or null if file not found.</returns>
    Task<PresignedUrlResult?> GetPublicUrlAsync(Guid id, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a presigned URL for an image preview with optional resizing.
    /// </summary>
    /// <param name="id">The file ID.</param>
    /// <param name="width">Optional width for resizing.</param>
    /// <param name="height">Optional height for resizing.</param>
    /// <param name="expiry">Optional expiry duration. Uses default if not specified.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Presigned URL result with resize parameters or null if file not found.</returns>
    Task<PresignedUrlResult?> GetPreviewUrlAsync(Guid id, int? width = null, int? height = null, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    Task<StoredFile> UploadAsync(UploadFileRequest request, CancellationToken cancellationToken = default);

    Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FileDownloadResult?> DownloadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<StoredFile> Items, int TotalCount)> ListAsync(int offset = 0, int limit = 50, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all files matching a metadata key-value pair for deletion.
    /// </summary>
    /// <param name="metadataKey">The metadata key to match.</param>
    /// <param name="metadataValue">The metadata value to match.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of files marked for deletion.</returns>
    Task<int> MarkForDeletionByMetadataAsync(string metadataKey, string metadataValue, CancellationToken cancellationToken = default);
}
