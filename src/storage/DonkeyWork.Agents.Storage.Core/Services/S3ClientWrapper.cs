using Amazon.S3;
using Amazon.S3.Model;
using DonkeyWork.Agents.Storage.Contracts.Services;
using DonkeyWork.Agents.Storage.Core.Options;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Storage.Core.Services;

public sealed class S3ClientWrapper : IS3ClientWrapper, IDisposable
{
    private readonly IAmazonS3 _s3Client;

    public S3ClientWrapper(IOptions<StorageOptions> options)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = options.Value.ServiceUrl,
            ForcePathStyle = options.Value.UsePathStyleAddressing
        };

        _s3Client = new AmazonS3Client(
            options.Value.AccessKey,
            options.Value.SecretKey,
            config);
    }

    public async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.GetBucketLocationAsync(bucketName, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task CreateBucketAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        await _s3Client.PutBucketAsync(bucketName, cancellationToken);
    }

    public async Task UploadAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream?> DownloadAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken);
    }

    public void Dispose()
    {
        _s3Client.Dispose();
    }
}
