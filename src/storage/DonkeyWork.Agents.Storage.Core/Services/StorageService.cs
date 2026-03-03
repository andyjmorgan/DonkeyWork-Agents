using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using DonkeyWork.Agents.Storage.Core.Options;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Storage.Core.Services;

public sealed class StorageService : IStorageService
{
    private readonly IS3ClientWrapper _s3Client;
    private readonly IIdentityContext _identityContext;
    private readonly StorageOptions _options;

    public StorageService(
        IS3ClientWrapper s3Client,
        IIdentityContext identityContext,
        IOptions<StorageOptions> options)
    {
        _s3Client = s3Client;
        _identityContext = identityContext;
        _options = options.Value;
    }

    public async Task<StorageUploadResult> UploadAsync(UploadFileRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _s3Client.BucketExistsAsync(_options.DefaultBucket, cancellationToken))
        {
            await _s3Client.CreateBucketAsync(_options.DefaultBucket, cancellationToken);
        }

        // Build object key: {userId}/{keyPrefix}/{filename} or {userId}/{filename}
        var objectKey = string.IsNullOrEmpty(request.KeyPrefix)
            ? $"{_identityContext.UserId}/{request.FileName}"
            : $"{_identityContext.UserId}/{request.KeyPrefix}/{request.FileName}";

        // Buffer to get size
        long sizeBytes;
        using (var ms = new MemoryStream())
        {
            await request.Content.CopyToAsync(ms, cancellationToken);
            sizeBytes = ms.Length;
            ms.Position = 0;
            await _s3Client.UploadAsync(_options.DefaultBucket, objectKey, ms, request.ContentType, cancellationToken);
        }

        return new StorageUploadResult
        {
            ObjectKey = objectKey,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = sizeBytes
        };
    }

    public async Task<IReadOnlyList<FileItemV1>> ListAsync(CancellationToken cancellationToken = default)
    {
        var prefix = $"{_identityContext.UserId}/";

        // Use delimiter "/" to get top-level files only (excludes conversations/ subfolder contents)
        var objects = await _s3Client.ListObjectsAsync(_options.DefaultBucket, prefix, "/", cancellationToken);

        return objects
            .Select(o => new FileItemV1
            {
                FileName = Path.GetFileName(o.Key),
                SizeBytes = o.SizeBytes,
                LastModified = o.LastModified
            })
            .OrderByDescending(f => f.LastModified)
            .ToList();
    }

    public async Task<FileDownloadResult?> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var fullKey = ResolveKey(objectKey);
        var metadata = await _s3Client.GetObjectMetadataAsync(_options.DefaultBucket, fullKey, cancellationToken);

        if (metadata == null)
            return null;

        var stream = await _s3Client.DownloadAsync(_options.DefaultBucket, fullKey, cancellationToken);
        if (stream == null)
            return null;

        return new FileDownloadResult
        {
            Content = stream,
            FileName = Path.GetFileName(objectKey),
            ContentType = metadata.ContentType,
            SizeBytes = metadata.SizeBytes
        };
    }

    public async Task<bool> DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var fullKey = ResolveKey(objectKey);
        var metadata = await _s3Client.GetObjectMetadataAsync(_options.DefaultBucket, fullKey, cancellationToken);

        if (metadata == null)
            return false;

        await _s3Client.DeleteAsync(_options.DefaultBucket, fullKey, cancellationToken);
        return true;
    }

    public async Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var fullPrefix = $"{_identityContext.UserId}/{prefix}";
        await _s3Client.DeleteByPrefixAsync(_options.DefaultBucket, fullPrefix, cancellationToken);
    }

    public async Task<PresignedUrlResult?> GetPublicUrlAsync(string objectKey, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var fullKey = ResolveKey(objectKey);
        var metadata = await _s3Client.GetObjectMetadataAsync(_options.DefaultBucket, fullKey, cancellationToken);

        if (metadata == null)
            return null;

        var urlExpiry = expiry ?? _options.DefaultPresignedUrlExpiry;
        var presignedUrl = _s3Client.GetPreSignedUrl(_options.DefaultBucket, fullKey, urlExpiry);

        if (!string.IsNullOrEmpty(_options.PublicServiceUrl))
        {
            var internalUri = new Uri(_options.ServiceUrl);
            var publicUri = new Uri(_options.PublicServiceUrl);
            presignedUrl = presignedUrl.Replace(
                $"{internalUri.Scheme}://{internalUri.Authority}",
                $"{publicUri.Scheme}://{publicUri.Authority}");
        }

        return new PresignedUrlResult
        {
            Url = presignedUrl,
            ExpiresAt = DateTimeOffset.UtcNow.Add(urlExpiry)
        };
    }

    public async Task<PresignedUrlResult?> GetPreviewUrlAsync(string objectKey, int? width = null, int? height = null, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var result = await GetPublicUrlAsync(objectKey, expiry, cancellationToken);
        if (result == null)
            return null;

        if (width.HasValue || height.HasValue)
        {
            var separator = result.Url.Contains('?') ? "&" : "?";
            var resizeParams = new List<string>();

            if (width.HasValue)
                resizeParams.Add($"width={width.Value}");
            if (height.HasValue)
                resizeParams.Add($"height={height.Value}");

            resizeParams.Add("mode=fit");
            result.Url = $"{result.Url}{separator}{string.Join("&", resizeParams)}";
        }

        return result;
    }

    /// <summary>
    /// Resolves a relative object key to a full key by prepending the user ID.
    /// </summary>
    private string ResolveKey(string objectKey)
    {
        return $"{_identityContext.UserId}/{objectKey}";
    }
}
