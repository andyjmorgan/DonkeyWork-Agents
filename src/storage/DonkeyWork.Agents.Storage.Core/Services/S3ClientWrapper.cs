using Amazon.S3;
using Amazon.S3.Model;
using DonkeyWork.Agents.Storage.Contracts.Models;
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

    public async Task<List<S3ObjectInfo>> ListObjectsAsync(string bucketName, string prefix, string? delimiter = null, CancellationToken cancellationToken = default)
    {
        var results = new List<S3ObjectInfo>();
        string? continuationToken = null;

        do
        {
            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix,
                Delimiter = delimiter,
                ContinuationToken = continuationToken
            };

            var response = await _s3Client.ListObjectsV2Async(request, cancellationToken);

            foreach (var obj in response.S3Objects)
            {
                results.Add(new S3ObjectInfo
                {
                    Key = obj.Key,
                    SizeBytes = obj.Size,
                    LastModified = new DateTimeOffset(obj.LastModified, TimeSpan.Zero)
                });
            }

            continuationToken = response.IsTruncated ? response.NextContinuationToken : null;
        } while (continuationToken != null);

        return results;
    }

    public async Task<S3ObjectMetadata?> GetObjectMetadataAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, cancellationToken);

            return new S3ObjectMetadata
            {
                ContentType = response.Headers.ContentType,
                SizeBytes = response.ContentLength,
                LastModified = new DateTimeOffset(response.LastModified, TimeSpan.Zero)
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteByPrefixAsync(string bucketName, string prefix, CancellationToken cancellationToken = default)
    {
        var objects = await ListObjectsAsync(bucketName, prefix, cancellationToken: cancellationToken);

        if (objects.Count == 0)
            return;

        // Delete in batches of 1000 (S3 limit)
        foreach (var batch in objects.Chunk(1000))
        {
            var deleteRequest = new DeleteObjectsRequest
            {
                BucketName = bucketName,
                Objects = batch.Select(o => new KeyVersion { Key = o.Key }).ToList()
            };

            await _s3Client.DeleteObjectsAsync(deleteRequest, cancellationToken);
        }
    }

    public string GetPreSignedUrl(string bucketName, string objectKey, TimeSpan expiry)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public void Dispose()
    {
        _s3Client.Dispose();
    }
}
