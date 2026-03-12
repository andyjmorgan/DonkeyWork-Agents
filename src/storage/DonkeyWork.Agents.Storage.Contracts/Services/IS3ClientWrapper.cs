using DonkeyWork.Agents.Storage.Contracts.Models;

namespace DonkeyWork.Agents.Storage.Contracts.Services;

public interface IS3ClientWrapper
{
    Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default);

    Task CreateBucketAsync(string bucketName, CancellationToken cancellationToken = default);

    Task UploadAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream?> DownloadAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists objects in a bucket with the given prefix, returning both objects and common prefixes (folders).
    /// </summary>
    Task<(List<S3ObjectInfo> Objects, List<string> CommonPrefixes)> ListObjectsAsync(string bucketName, string prefix, string? delimiter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a specific object (HeadObject).
    /// </summary>
    Task<S3ObjectMetadata?> GetObjectMetadataAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all objects matching a prefix (list + batch delete).
    /// </summary>
    Task DeleteByPrefixAsync(string bucketName, string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned URL for direct access to an object.
    /// </summary>
    string GetPreSignedUrl(string bucketName, string objectKey, TimeSpan expiry);
}
