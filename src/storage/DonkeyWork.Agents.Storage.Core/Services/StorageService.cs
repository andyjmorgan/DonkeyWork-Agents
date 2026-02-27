using System.Security.Cryptography;
using System.Text.Json;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Storage;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using DonkeyWork.Agents.Storage.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Storage.Core.Services;

public sealed class StorageService : IStorageService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IS3ClientWrapper _s3Client;
    private readonly IIdentityContext _identityContext;
    private readonly StorageOptions _options;

    public StorageService(
        AgentsDbContext dbContext,
        IS3ClientWrapper s3Client,
        IIdentityContext identityContext,
        IOptions<StorageOptions> options)
    {
        _dbContext = dbContext;
        _s3Client = s3Client;
        _identityContext = identityContext;
        _options = options.Value;
    }

    public async Task<PresignedUrlResult?> GetPublicUrlAsync(Guid id, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == id && f.Status == FileStatus.Active, cancellationToken);

        if (entity == null)
            return null;

        var urlExpiry = expiry ?? _options.DefaultPresignedUrlExpiry;
        var presignedUrl = _s3Client.GetPreSignedUrl(entity.BucketName, entity.ObjectKey, urlExpiry);

        // If a public service URL is configured, replace the internal URL
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

    public async Task<PresignedUrlResult?> GetPreviewUrlAsync(Guid id, int? width = null, int? height = null, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var result = await GetPublicUrlAsync(id, expiry, cancellationToken);
        if (result == null)
            return null;

        // SeaweedFS supports image resizing via query parameters
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

    public async Task<StoredFile> UploadAsync(UploadFileRequest request, CancellationToken cancellationToken = default)
    {
        // Ensure bucket exists
        if (!await _s3Client.BucketExistsAsync(_options.DefaultBucket, cancellationToken))
        {
            await _s3Client.CreateBucketAsync(_options.DefaultBucket, cancellationToken);
        }

        // Generate unique object key
        var objectKey = $"{_identityContext.UserId}/{Guid.NewGuid()}/{request.FileName}";

        // Calculate checksum
        string? checksum = null;
        long sizeBytes;

        using (var ms = new MemoryStream())
        {
            await request.Content.CopyToAsync(ms, cancellationToken);
            sizeBytes = ms.Length;

            ms.Position = 0;
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(ms, cancellationToken);
            checksum = Convert.ToHexString(hashBytes).ToLowerInvariant();

            ms.Position = 0;
            await _s3Client.UploadAsync(_options.DefaultBucket, objectKey, ms, request.ContentType, cancellationToken);
        }

        // Create database record
        var entity = new StoredFileEntity
        {
            UserId = _identityContext.UserId,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = sizeBytes,
            BucketName = _options.DefaultBucket,
            ObjectKey = objectKey,
            ChecksumSha256 = checksum,
            Status = FileStatus.Active,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
        };

        _dbContext.StoredFiles.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToModel(entity);
    }

    public async Task<StoredFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        return entity == null ? null : ToModel(entity);
    }

    public async Task<FileDownloadResult?> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == id && f.Status == FileStatus.Active, cancellationToken);

        if (entity == null)
            return null;

        var stream = await _s3Client.DownloadAsync(entity.BucketName, entity.ObjectKey, cancellationToken);
        if (stream == null)
            return null;

        return new FileDownloadResult
        {
            Content = stream,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            SizeBytes = entity.SizeBytes
        };
    }

    public async Task<(IReadOnlyList<StoredFile> Items, int TotalCount)> ListAsync(int offset = 0, int limit = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.StoredFiles
            .Where(f => f.Status == FileStatus.Active)
            .OrderByDescending(f => f.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var items = entities.Select(ToModel).ToList();
        return (items, totalCount);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == id && f.Status == FileStatus.Active, cancellationToken);

        if (entity == null)
            return false;

        // Soft delete - mark for deletion
        entity.Status = FileStatus.MarkedForDeletion;
        entity.MarkedForDeletionAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> MarkForDeletionByMetadataAsync(string metadataKey, string metadataValue, CancellationToken cancellationToken = default)
    {
        var searchPattern = $"\"{metadataKey}\":\"{metadataValue}\"";
        var now = DateTimeOffset.UtcNow;

        // Fetch active files with metadata, then filter in memory.
        // string.Contains translates to LIKE which PostgreSQL rejects on jsonb columns.
        var candidates = await _dbContext.StoredFiles
            .Where(f => f.Status == FileStatus.Active &&
                        f.Metadata != null)
            .ToListAsync(cancellationToken);

        var entities = candidates.Where(f => f.Metadata!.Contains(searchPattern)).ToList();

        if (entities.Count == 0)
            return 0;

        // Mark each file for deletion
        foreach (var entity in entities)
        {
            entity.Status = FileStatus.MarkedForDeletion;
            entity.MarkedForDeletionAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return entities.Count;
    }

    private static StoredFile ToModel(StoredFileEntity entity)
    {
        return new StoredFile
        {
            Id = entity.Id,
            UserId = entity.UserId,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            SizeBytes = entity.SizeBytes,
            BucketName = entity.BucketName,
            ObjectKey = entity.ObjectKey,
            ChecksumSha256 = entity.ChecksumSha256,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            MarkedForDeletionAt = entity.MarkedForDeletionAt,
            Metadata = string.IsNullOrEmpty(entity.Metadata)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Metadata)
        };
    }
}
