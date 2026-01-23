using System.Security.Cryptography;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Storage;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using DonkeyWork.Agents.Storage.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FileShareModel = DonkeyWork.Agents.Storage.Contracts.Models.FileShare;

namespace DonkeyWork.Agents.Storage.Core.Services;

public sealed class FileShareService : IFileShareService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IS3ClientWrapper _s3Client;
    private readonly IIdentityContext _identityContext;
    private readonly StorageOptions _options;

    public FileShareService(
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

    public async Task<FileShareModel> CreateShareAsync(CreateShareRequest request, CancellationToken cancellationToken = default)
    {
        // Verify file exists and belongs to user
        var file = await _dbContext.StoredFiles
            .FirstOrDefaultAsync(f => f.Id == request.FileId && f.Status == FileStatus.Active, cancellationToken);

        if (file == null)
            throw new InvalidOperationException("File not found or not accessible");

        var expiresIn = request.ExpiresIn ?? _options.DefaultShareExpiry;
        var token = GenerateShareToken();

        string? passwordHash = null;
        if (!string.IsNullOrEmpty(request.Password))
        {
            passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        var entity = new FileShareEntity
        {
            UserId = _identityContext.UserId,
            FileId = request.FileId,
            ShareToken = token,
            ExpiresAt = DateTimeOffset.UtcNow.Add(expiresIn),
            Status = ShareStatus.Active,
            MaxDownloads = request.MaxDownloads,
            DownloadCount = 0,
            PasswordHash = passwordHash
        };

        _dbContext.FileShares.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToModel(entity);
    }

    public async Task<FileShareModel?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.FileShares
            .IgnoreQueryFilters() // Shares should be accessible by anyone with the token
            .Include(s => s.File)
            .FirstOrDefaultAsync(s => s.ShareToken == token, cancellationToken);

        if (entity == null)
            return null;

        // Check if expired
        if (entity.Status == ShareStatus.Active && entity.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            entity.Status = ShareStatus.Expired;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToModel(entity);
    }

    public async Task<FileDownloadResult?> DownloadByTokenAsync(string token, string? password = null, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.FileShares
            .IgnoreQueryFilters()
            .Include(s => s.File)
            .FirstOrDefaultAsync(s => s.ShareToken == token, cancellationToken);

        if (entity == null)
            return null;

        // Check status
        if (entity.Status != ShareStatus.Active)
            return null;

        // Check expiry
        if (entity.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            entity.Status = ShareStatus.Expired;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        // Check max downloads
        if (entity.MaxDownloads.HasValue && entity.DownloadCount >= entity.MaxDownloads.Value)
            return null;

        // Check password
        if (!string.IsNullOrEmpty(entity.PasswordHash))
        {
            if (string.IsNullOrEmpty(password) || !BCrypt.Net.BCrypt.Verify(password, entity.PasswordHash))
                return null;
        }

        // Check file status
        if (entity.File.Status != FileStatus.Active)
            return null;

        // Download from S3
        var stream = await _s3Client.DownloadAsync(entity.File.BucketName, entity.File.ObjectKey, cancellationToken);
        if (stream == null)
            return null;

        // Increment download count
        entity.DownloadCount++;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new FileDownloadResult
        {
            Content = stream,
            FileName = entity.File.FileName,
            ContentType = entity.File.ContentType,
            SizeBytes = entity.File.SizeBytes
        };
    }

    public async Task<(IReadOnlyList<FileShareModel> Items, int TotalCount)> ListByFileAsync(Guid fileId, int offset = 0, int limit = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FileShares
            .Where(s => s.FileId == fileId)
            .OrderByDescending(s => s.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var items = entities.Select(ToModel).ToList();
        return (items, totalCount);
    }

    public async Task<bool> RevokeAsync(Guid shareId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.FileShares
            .FirstOrDefaultAsync(s => s.Id == shareId && s.Status == ShareStatus.Active, cancellationToken);

        if (entity == null)
            return false;

        entity.Status = ShareStatus.Revoked;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string GenerateShareToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private static FileShareModel ToModel(FileShareEntity entity)
    {
        return new FileShareModel
        {
            Id = entity.Id,
            FileId = entity.FileId,
            UserId = entity.UserId,
            ShareToken = entity.ShareToken,
            ExpiresAt = entity.ExpiresAt,
            Status = entity.Status,
            MaxDownloads = entity.MaxDownloads,
            DownloadCount = entity.DownloadCount,
            HasPassword = !string.IsNullOrEmpty(entity.PasswordHash),
            CreatedAt = entity.CreatedAt
        };
    }
}
