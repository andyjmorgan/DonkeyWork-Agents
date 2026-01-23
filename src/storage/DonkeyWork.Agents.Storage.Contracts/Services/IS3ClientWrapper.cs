namespace DonkeyWork.Agents.Storage.Contracts.Services;

public interface IS3ClientWrapper
{
    Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default);

    Task CreateBucketAsync(string bucketName, CancellationToken cancellationToken = default);

    Task UploadAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task<Stream?> DownloadAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default);
}
