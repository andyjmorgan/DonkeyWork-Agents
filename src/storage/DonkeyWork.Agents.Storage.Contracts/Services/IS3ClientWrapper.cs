namespace DonkeyWork.Agents.Storage.Contracts.Services;

public interface IS3ClientWrapper
{
    Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default);

    Task CreateBucketAsync(string bucketName, CancellationToken cancellationToken = default);

    Task UploadAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream?> DownloadAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned URL for direct access to an object.
    /// </summary>
    /// <param name="bucketName">The bucket containing the object.</param>
    /// <param name="objectKey">The object key.</param>
    /// <param name="expiry">How long the URL should be valid.</param>
    /// <returns>A presigned URL string.</returns>
    string GetPreSignedUrl(string bucketName, string objectKey, TimeSpan expiry);
}
