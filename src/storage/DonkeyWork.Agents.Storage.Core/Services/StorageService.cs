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

    private bool UseFilesystem => _options.FileSystemBasePath != null;

    private string GetUserDirectory()
    {
        return FileSystemPathHelper.GetUserDirectory(
            _options.FileSystemBasePath!,
            _options.UserFilesSubPath,
            _identityContext.UserId);
    }

    public async Task<StorageUploadResult> UploadAsync(UploadFileRequest request, CancellationToken cancellationToken = default)
    {
        // User file (no KeyPrefix) + filesystem configured → write to filesystem
        if (UseFilesystem && string.IsNullOrEmpty(request.KeyPrefix))
        {
            return await UploadToFilesystemAsync(request, cancellationToken);
        }

        return await UploadToS3Async(request, cancellationToken);
    }

    public async Task<FileListingResponseV1> ListAsync(string? prefix = null, CancellationToken cancellationToken = default)
    {
        if (UseFilesystem)
            return ListFromFilesystem(prefix);

        return await ListFromS3Async(prefix, cancellationToken);
    }

    public async Task<FileDownloadResult?> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (UseFilesystem && FileSystemPathHelper.IsUserFile(objectKey))
        {
            return DownloadFromFilesystem(objectKey);
        }

        return await DownloadFromS3Async(objectKey, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        if (UseFilesystem && FileSystemPathHelper.IsUserFile(objectKey))
        {
            return DeleteFromFilesystem(objectKey);
        }

        return await DeleteFromS3Async(objectKey, cancellationToken);
    }

    public async Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // Prefix-based deletion is always S3 (conversation cleanup, etc.)
        var fullPrefix = $"{_identityContext.UserId}/{prefix}";
        await _s3Client.DeleteByPrefixAsync(_options.DefaultBucket, fullPrefix, cancellationToken);
    }

    public async Task<PresignedUrlResult?> GetPublicUrlAsync(string objectKey, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (UseFilesystem && FileSystemPathHelper.IsUserFile(objectKey))
        {
            // No presigned URLs for filesystem files
            return null;
        }

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

    #region Filesystem Operations

    private async Task<StorageUploadResult> UploadToFilesystemAsync(UploadFileRequest request, CancellationToken cancellationToken)
    {
        var userDir = GetUserDirectory();
        string filePath;

        try
        {
            filePath = FileSystemPathHelper.GetSafeFilePath(userDir, request.FileName);
        }
        catch (ArgumentException)
        {
            throw;
        }

        Directory.CreateDirectory(userDir);

        long sizeBytes;
        await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await request.Content.CopyToAsync(fs, cancellationToken);
            sizeBytes = fs.Length;
        }

        return new StorageUploadResult
        {
            ObjectKey = $"{_identityContext.UserId}/{request.FileName}",
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = sizeBytes
        };
    }

    private FileListingResponseV1 ListFromFilesystem(string? prefix)
    {
        var userDir = GetUserDirectory();
        var targetDir = string.IsNullOrEmpty(prefix)
            ? userDir
            : Path.Combine(userDir, prefix);

        // Validate targetDir is still under userDir (path traversal protection)
        var fullTarget = Path.GetFullPath(targetDir);
        var fullUserDir = Path.GetFullPath(userDir);
        if (!fullTarget.StartsWith(fullUserDir, StringComparison.OrdinalIgnoreCase))
            return new FileListingResponseV1 { Files = [], Folders = [] };

        if (!Directory.Exists(fullTarget))
            return new FileListingResponseV1 { Files = [], Folders = [] };

        var dirInfo = new DirectoryInfo(fullTarget);

        var files = dirInfo.GetFiles()
            .Select(f => new FileItemV1
            {
                FileName = f.Name,
                SizeBytes = f.Length,
                LastModified = new DateTimeOffset(f.LastWriteTimeUtc, TimeSpan.Zero)
            })
            .OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var folders = dirInfo.GetDirectories()
            .Select(d => d.Name)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new FileListingResponseV1 { Files = files, Folders = folders };
    }

    private FileDownloadResult? DownloadFromFilesystem(string objectKey)
    {
        var userDir = GetUserDirectory();

        try
        {
            var filePath = FileSystemPathHelper.GetSafeFilePath(userDir, objectKey);

            if (!File.Exists(filePath))
                return null;

            var fileInfo = new FileInfo(filePath);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return new FileDownloadResult
            {
                Content = stream,
                FileName = fileInfo.Name,
                ContentType = FileSystemPathHelper.GetContentType(fileInfo.Name),
                SizeBytes = fileInfo.Length
            };
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private bool DeleteFromFilesystem(string objectKey)
    {
        var userDir = GetUserDirectory();

        try
        {
            var filePath = FileSystemPathHelper.GetSafeFilePath(userDir, objectKey);

            if (!File.Exists(filePath))
                return false;

            File.Delete(filePath);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    #endregion

    #region S3 Operations

    private async Task<StorageUploadResult> UploadToS3Async(UploadFileRequest request, CancellationToken cancellationToken)
    {
        if (!await _s3Client.BucketExistsAsync(_options.DefaultBucket, cancellationToken))
        {
            await _s3Client.CreateBucketAsync(_options.DefaultBucket, cancellationToken);
        }

        var objectKey = string.IsNullOrEmpty(request.KeyPrefix)
            ? $"{_identityContext.UserId}/{request.FileName}"
            : $"{_identityContext.UserId}/{request.KeyPrefix}/{request.FileName}";

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

    private async Task<FileListingResponseV1> ListFromS3Async(string? prefix, CancellationToken cancellationToken)
    {
        var s3Prefix = $"{_identityContext.UserId}/";
        if (!string.IsNullOrEmpty(prefix))
            s3Prefix += prefix.TrimEnd('/') + "/";

        var (objects, commonPrefixes) = await _s3Client.ListObjectsAsync(_options.DefaultBucket, s3Prefix, "/", cancellationToken);

        var files = objects
            .Select(o => new FileItemV1
            {
                FileName = Path.GetFileName(o.Key),
                SizeBytes = o.SizeBytes,
                LastModified = o.LastModified
            })
            .Where(f => !string.IsNullOrEmpty(f.FileName))
            .OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Extract folder names from common prefixes (e.g. "{userId}/folder/" -> "folder")
        var folders = commonPrefixes
            .Select(p => p[s3Prefix.Length..].TrimEnd('/'))
            .Where(n => !string.IsNullOrEmpty(n))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new FileListingResponseV1 { Files = files, Folders = folders };
    }

    private async Task<FileDownloadResult?> DownloadFromS3Async(string objectKey, CancellationToken cancellationToken)
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

    private async Task<bool> DeleteFromS3Async(string objectKey, CancellationToken cancellationToken)
    {
        var fullKey = ResolveKey(objectKey);
        var metadata = await _s3Client.GetObjectMetadataAsync(_options.DefaultBucket, fullKey, cancellationToken);

        if (metadata == null)
            return false;

        await _s3Client.DeleteAsync(_options.DefaultBucket, fullKey, cancellationToken);
        return true;
    }

    #endregion

    private string ResolveKey(string objectKey)
    {
        return $"{_identityContext.UserId}/{objectKey}";
    }
}
