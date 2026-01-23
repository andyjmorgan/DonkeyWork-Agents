using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Storage;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using DonkeyWork.Agents.Storage.Core.Options;
using DonkeyWork.Agents.Storage.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Storage.Tests.Services;

public class FileShareServiceTests
{
    private readonly Mock<IS3ClientWrapper> _s3ClientMock;
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly IOptions<StorageOptions> _options;
    private readonly Guid _userId = Guid.NewGuid();

    public FileShareServiceTests()
    {
        _s3ClientMock = new Mock<IS3ClientWrapper>();
        _identityContextMock = new Mock<IIdentityContext>();
        _identityContextMock.Setup(x => x.UserId).Returns(_userId);

        _options = Options.Create(new StorageOptions
        {
            ServiceUrl = "http://localhost:8333",
            AccessKey = "admin",
            SecretKey = "admin",
            DefaultBucket = "test-bucket",
            DefaultShareExpiry = TimeSpan.FromDays(1)
        });
    }

    private AgentsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AgentsDbContext(options, _identityContextMock.Object);
    }

    private StoredFileEntity CreateTestFile(AgentsDbContext dbContext)
    {
        var entity = new StoredFileEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileName = "test.txt",
            ContentType = "text/plain",
            SizeBytes = 100,
            BucketName = "test-bucket",
            ObjectKey = "test-key",
            Status = FileStatus.Active
        };
        dbContext.StoredFiles.Add(entity);
        dbContext.SaveChanges();
        return entity;
    }

    [Fact]
    public async Task CreateShareAsync_ValidFile_ReturnsShare()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var file = CreateTestFile(dbContext);
        var service = new FileShareService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        var request = new CreateShareRequest
        {
            FileId = file.Id,
            ExpiresIn = TimeSpan.FromHours(1)
        };

        // Act
        var result = await service.CreateShareAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(file.Id, result.FileId);
        Assert.Equal(_userId, result.UserId);
        Assert.Equal(ShareStatus.Active, result.Status);
        Assert.NotEmpty(result.ShareToken);
        Assert.False(result.HasPassword);
    }

    [Fact]
    public async Task CreateShareAsync_WithPassword_HasPasswordIsTrue()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var file = CreateTestFile(dbContext);
        var service = new FileShareService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        var request = new CreateShareRequest
        {
            FileId = file.Id,
            Password = "secret123"
        };

        // Act
        var result = await service.CreateShareAsync(request);

        // Assert
        Assert.True(result.HasPassword);
    }

    [Fact]
    public async Task CreateShareAsync_NonExistingFile_ThrowsException()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = new FileShareService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        var request = new CreateShareRequest
        {
            FileId = Guid.NewGuid()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateShareAsync(request));
    }

    [Fact]
    public async Task GetByTokenAsync_ValidToken_ReturnsShare()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var file = CreateTestFile(dbContext);
        var shareEntity = new FileShareEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileId = file.Id,
            ShareToken = "test-token-123",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Status = ShareStatus.Active
        };
        dbContext.FileShares.Add(shareEntity);
        await dbContext.SaveChangesAsync();

        var service = new FileShareService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var result = await service.GetByTokenAsync("test-token-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(shareEntity.Id, result.Id);
        Assert.Equal("test-token-123", result.ShareToken);
    }

    [Fact]
    public async Task GetByTokenAsync_ExpiredShare_UpdatesStatus()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var file = CreateTestFile(dbContext);
        var shareEntity = new FileShareEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileId = file.Id,
            ShareToken = "expired-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1), // Expired
            Status = ShareStatus.Active
        };
        dbContext.FileShares.Add(shareEntity);
        await dbContext.SaveChangesAsync();

        var service = new FileShareService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var result = await service.GetByTokenAsync("expired-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ShareStatus.Expired, result.Status);
    }

    [Fact]
    public async Task DownloadByTokenAsync_ValidShare_ReturnsContent()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var file = CreateTestFile(dbContext);
        var shareEntity = new FileShareEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileId = file.Id,
            ShareToken = "download-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Status = ShareStatus.Active,
            DownloadCount = 0
        };
        dbContext.FileShares.Add(shareEntity);
        await dbContext.SaveChangesAsync();

        var contentStream = new MemoryStream("test content"u8.ToArray());
        _s3ClientMock.Setup(x => x.DownloadAsync("test-bucket", "test-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentStream);

        var service = new FileShareService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var result = await service.DownloadByTokenAsync("download-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.txt", result.FileName);

        // Verify download count incremented
        var updatedShare = await dbContext.FileShares.IgnoreQueryFilters().FirstAsync(s => s.Id == shareEntity.Id);
        Assert.Equal(1, updatedShare.DownloadCount);
    }

    [Fact]
    public async Task DownloadByTokenAsync_WithPassword_RequiresCorrectPassword()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var file = CreateTestFile(dbContext);
        var shareEntity = new FileShareEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileId = file.Id,
            ShareToken = "password-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Status = ShareStatus.Active,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret123")
        };
        dbContext.FileShares.Add(shareEntity);
        await dbContext.SaveChangesAsync();

        var service = new FileShareService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act - wrong password
        var resultWrongPassword = await service.DownloadByTokenAsync("password-token", "wrongpassword");

        // Assert
        Assert.Null(resultWrongPassword);

        // Act - correct password
        var contentStream = new MemoryStream("test content"u8.ToArray());
        _s3ClientMock.Setup(x => x.DownloadAsync("test-bucket", "test-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentStream);

        var resultCorrectPassword = await service.DownloadByTokenAsync("password-token", "secret123");

        // Assert
        Assert.NotNull(resultCorrectPassword);
    }

    [Fact]
    public async Task DownloadByTokenAsync_MaxDownloadsReached_ReturnsNull()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var file = CreateTestFile(dbContext);
        var shareEntity = new FileShareEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileId = file.Id,
            ShareToken = "limited-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Status = ShareStatus.Active,
            MaxDownloads = 1,
            DownloadCount = 1 // Already reached max
        };
        dbContext.FileShares.Add(shareEntity);
        await dbContext.SaveChangesAsync();

        var service = new FileShareService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var result = await service.DownloadByTokenAsync("limited-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RevokeAsync_ActiveShare_RevokesShare()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var file = CreateTestFile(dbContext);
        var shareEntity = new FileShareEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileId = file.Id,
            ShareToken = "revoke-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Status = ShareStatus.Active
        };
        dbContext.FileShares.Add(shareEntity);
        await dbContext.SaveChangesAsync();

        var service = new FileShareService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var result = await service.RevokeAsync(shareEntity.Id);

        // Assert
        Assert.True(result);
        var updatedShare = await dbContext.FileShares.IgnoreQueryFilters().FirstAsync(s => s.Id == shareEntity.Id);
        Assert.Equal(ShareStatus.Revoked, updatedShare.Status);
    }

    [Fact]
    public async Task ListByFileAsync_ReturnsSharesForFile()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var file = CreateTestFile(dbContext);
        var share1 = new FileShareEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileId = file.Id,
            ShareToken = "share-1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Status = ShareStatus.Active
        };
        var share2 = new FileShareEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileId = file.Id,
            ShareToken = "share-2",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Status = ShareStatus.Active
        };
        dbContext.FileShares.AddRange(share1, share2);
        await dbContext.SaveChangesAsync();

        var service = new FileShareService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var (items, totalCount) = await service.ListByFileAsync(file.Id);

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Equal(2, totalCount);
    }
}
