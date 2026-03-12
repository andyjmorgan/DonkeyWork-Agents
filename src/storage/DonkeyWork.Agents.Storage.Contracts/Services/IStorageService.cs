using DonkeyWork.Agents.Storage.Contracts.Models;

namespace DonkeyWork.Agents.Storage.Contracts.Services;

public interface IStorageService
{
    /// <summary>
    /// Uploads a file to S3 storage.
    /// </summary>
    Task<StorageUploadResult> UploadAsync(UploadFileRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files and folders for the current user, optionally within a prefix (subfolder).
    /// </summary>
    Task<FileListingResponseV1> ListAsync(string? prefix = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file by object key (relative to user namespace).
    /// </summary>
    Task<FileDownloadResult?> DownloadAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file by object key (relative to user namespace).
    /// </summary>
    Task<bool> DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all objects matching a prefix within the user's namespace.
    /// </summary>
    Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a presigned URL for direct file access.
    /// </summary>
    Task<PresignedUrlResult?> GetPublicUrlAsync(string objectKey, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a presigned URL for an image preview with optional resizing.
    /// </summary>
    Task<PresignedUrlResult?> GetPreviewUrlAsync(string objectKey, int? width = null, int? height = null, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
}
