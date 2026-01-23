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

public class StorageServiceTests
{
    private readonly Mock<IS3ClientWrapper> _s3ClientMock;
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly IOptions<StorageOptions> _options;
    private readonly Guid _userId = Guid.NewGuid();

    public StorageServiceTests()
    {
        _s3ClientMock = new Mock<IS3ClientWrapper>();
        _identityContextMock = new Mock<IIdentityContext>();
        _identityContextMock.Setup(x => x.UserId).Returns(_userId);

        _options = Options.Create(new StorageOptions
        {
            ServiceUrl = "http://localhost:8333",
            AccessKey = "admin",
            SecretKey = "admin",
            DefaultBucket = "test-bucket"
        });
    }

    private AgentsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AgentsDbContext(options, _identityContextMock.Object);
    }

    [Fact]
    public async Task UploadAsync_ValidFile_ReturnsStoredFile()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = new StorageService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        _s3ClientMock.Setup(x => x.BucketExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _s3ClientMock.Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var content = new MemoryStream("test content"u8.ToArray());
        var request = new UploadFileRequest
        {
            FileName = "test.txt",
            ContentType = "text/plain",
            Content = content
        };

        // Act
        var result = await service.UploadAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.txt", result.FileName);
        Assert.Equal("text/plain", result.ContentType);
        Assert.Equal(_userId, result.UserId);
        Assert.Equal(FileStatus.Active, result.Status);
        Assert.NotNull(result.ChecksumSha256);
    }

    [Fact]
    public async Task UploadAsync_BucketNotExists_CreatesBucket()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = new StorageService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        _s3ClientMock.Setup(x => x.BucketExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _s3ClientMock.Setup(x => x.CreateBucketAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _s3ClientMock.Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var content = new MemoryStream("test content"u8.ToArray());
        var request = new UploadFileRequest
        {
            FileName = "test.txt",
            ContentType = "text/plain",
            Content = content
        };

        // Act
        await service.UploadAsync(request);

        // Assert
        _s3ClientMock.Verify(x => x.CreateBucketAsync("test-bucket", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingFile_ReturnsFile()
    {
        // Arrange
        var dbContext = CreateDbContext();
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
        await dbContext.SaveChangesAsync();

        var service = new StorageService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var result = await service.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal("test.txt", result.FileName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingFile_ReturnsNull()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = new StorageService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyActiveFiles()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var activeFile = new StoredFileEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileName = "active.txt",
            ContentType = "text/plain",
            SizeBytes = 100,
            BucketName = "test-bucket",
            ObjectKey = "active-key",
            Status = FileStatus.Active
        };
        var deletedFile = new StoredFileEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileName = "deleted.txt",
            ContentType = "text/plain",
            SizeBytes = 100,
            BucketName = "test-bucket",
            ObjectKey = "deleted-key",
            Status = FileStatus.MarkedForDeletion
        };
        dbContext.StoredFiles.AddRange(activeFile, deletedFile);
        await dbContext.SaveChangesAsync();

        var service = new StorageService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var (items, totalCount) = await service.ListAsync();

        // Assert
        Assert.Single(items);
        Assert.Equal(1, totalCount);
        Assert.Equal("active.txt", items[0].FileName);
    }

    [Fact]
    public async Task DeleteAsync_ExistingFile_MarksForDeletion()
    {
        // Arrange
        var dbContext = CreateDbContext();
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
        await dbContext.SaveChangesAsync();

        var service = new StorageService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var result = await service.DeleteAsync(entity.Id);

        // Assert
        Assert.True(result);
        var updatedEntity = await dbContext.StoredFiles.IgnoreQueryFilters().FirstAsync(f => f.Id == entity.Id);
        Assert.Equal(FileStatus.MarkedForDeletion, updatedEntity.Status);
        Assert.NotNull(updatedEntity.MarkedForDeletionAt);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingFile_ReturnsFalse()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var service = new StorageService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DownloadAsync_ExistingFile_ReturnsFileContent()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var entity = new StoredFileEntity
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            FileName = "test.txt",
            ContentType = "text/plain",
            SizeBytes = 12,
            BucketName = "test-bucket",
            ObjectKey = "test-key",
            Status = FileStatus.Active
        };
        dbContext.StoredFiles.Add(entity);
        await dbContext.SaveChangesAsync();

        var contentStream = new MemoryStream("test content"u8.ToArray());
        _s3ClientMock.Setup(x => x.DownloadAsync("test-bucket", "test-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentStream);

        var service = new StorageService(dbContext, _s3ClientMock.Object, _identityContextMock.Object, _options);

        // Act
        var result = await service.DownloadAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.txt", result.FileName);
        Assert.Equal("text/plain", result.ContentType);
    }
}
