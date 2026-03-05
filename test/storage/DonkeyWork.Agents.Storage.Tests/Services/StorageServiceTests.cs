using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Storage.Contracts.Models;
using DonkeyWork.Agents.Storage.Contracts.Services;
using DonkeyWork.Agents.Storage.Core.Options;
using DonkeyWork.Agents.Storage.Core.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Storage.Tests.Services;

public class StorageServiceTests : IDisposable
{
    private readonly Mock<IS3ClientWrapper> _s3ClientMock;
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly IOptions<StorageOptions> _options;
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly string _tempDir;

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

        _tempDir = Path.Combine(Path.GetTempPath(), "storage-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private StorageService CreateService(IOptions<StorageOptions>? options = null)
    {
        return new StorageService(_s3ClientMock.Object, _identityContextMock.Object, options ?? _options);
    }

    private IOptions<StorageOptions> CreateFsOptions()
    {
        return Options.Create(new StorageOptions
        {
            ServiceUrl = "http://localhost:8333",
            AccessKey = "admin",
            SecretKey = "admin",
            DefaultBucket = "test-bucket",
            FileSystemBasePath = _tempDir,
            UserFilesSubPath = "files"
        });
    }

    private string GetUserDir()
    {
        return Path.Combine(_tempDir, "files", _userId.ToString());
    }

    #region UploadAsync Tests (S3)

    [Fact]
    public async Task UploadAsync_ValidFile_ReturnsUploadResult()
    {
        // Arrange
        var service = CreateService();
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
        Assert.Equal(12, result.SizeBytes);
        Assert.Equal($"{_userId}/test.txt", result.ObjectKey);
    }

    [Fact]
    public async Task UploadAsync_WithKeyPrefix_IncludesPrefixInObjectKey()
    {
        // Arrange
        var service = CreateService();
        _s3ClientMock.Setup(x => x.BucketExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _s3ClientMock.Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var convId = Guid.NewGuid();
        var content = new MemoryStream("image data"u8.ToArray());
        var request = new UploadFileRequest
        {
            FileName = "photo.png",
            ContentType = "image/png",
            Content = content,
            KeyPrefix = $"conversations/{convId}"
        };

        // Act
        var result = await service.UploadAsync(request);

        // Assert
        Assert.Equal($"{_userId}/conversations/{convId}/photo.png", result.ObjectKey);
    }

    [Fact]
    public async Task UploadAsync_BucketNotExists_CreatesBucket()
    {
        // Arrange
        var service = CreateService();
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

    #endregion

    #region ListAsync Tests (S3)

    [Fact]
    public async Task ListAsync_ReturnsTopLevelFiles()
    {
        // Arrange
        var service = CreateService();
        var objects = new List<S3ObjectInfo>
        {
            new() { Key = $"{_userId}/file1.txt", SizeBytes = 100, LastModified = DateTimeOffset.UtcNow },
            new() { Key = $"{_userId}/file2.png", SizeBytes = 2000, LastModified = DateTimeOffset.UtcNow.AddHours(-1) }
        };

        _s3ClientMock.Setup(x => x.ListObjectsAsync("test-bucket", $"{_userId}/", "/", It.IsAny<CancellationToken>()))
            .ReturnsAsync(objects);

        // Act
        var result = await service.ListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("file1.txt", result[0].FileName);
        Assert.Equal("file2.png", result[1].FileName);
    }

    [Fact]
    public async Task ListAsync_EmptyBucket_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();
        _s3ClientMock.Setup(x => x.ListObjectsAsync("test-bucket", $"{_userId}/", "/", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<S3ObjectInfo>());

        // Act
        var result = await service.ListAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region DownloadAsync Tests (S3)

    [Fact]
    public async Task DownloadAsync_ExistingFile_ReturnsFileContent()
    {
        // Arrange
        var service = CreateService();
        var fullKey = $"{_userId}/test.txt";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata { ContentType = "text/plain", SizeBytes = 12, LastModified = DateTimeOffset.UtcNow });

        var contentStream = new MemoryStream("test content"u8.ToArray());
        _s3ClientMock.Setup(x => x.DownloadAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentStream);

        // Act
        var result = await service.DownloadAsync("test.txt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.txt", result.FileName);
        Assert.Equal("text/plain", result.ContentType);
        Assert.Equal(12, result.SizeBytes);
    }

    [Fact]
    public async Task DownloadAsync_NonExistingFile_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", $"{_userId}/missing.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync((S3ObjectMetadata?)null);

        // Act
        var result = await service.DownloadAsync("missing.txt");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync Tests (S3)

    [Fact]
    public async Task DeleteAsync_ExistingFile_DeletesAndReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var fullKey = $"{_userId}/test.txt";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata { ContentType = "text/plain", SizeBytes = 100, LastModified = DateTimeOffset.UtcNow });
        _s3ClientMock.Setup(x => x.DeleteAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.DeleteAsync("test.txt");

        // Assert
        Assert.True(result);
        _s3ClientMock.Verify(x => x.DeleteAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingFile_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", $"{_userId}/missing.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync((S3ObjectMetadata?)null);

        // Act
        var result = await service.DeleteAsync("missing.txt");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DeleteByPrefixAsync Tests

    [Fact]
    public async Task DeleteByPrefixAsync_DelegatesWithUserPrefix()
    {
        // Arrange
        var service = CreateService();
        var convId = Guid.NewGuid();
        var prefix = $"conversations/{convId}";

        _s3ClientMock.Setup(x => x.DeleteByPrefixAsync("test-bucket", $"{_userId}/{prefix}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.DeleteByPrefixAsync(prefix);

        // Assert
        _s3ClientMock.Verify(x => x.DeleteByPrefixAsync("test-bucket", $"{_userId}/{prefix}", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetPublicUrlAsync Tests (S3)

    [Fact]
    public async Task GetPublicUrlAsync_ExistingFile_ReturnsPresignedUrl()
    {
        // Arrange
        var service = CreateService();
        var fullKey = $"{_userId}/test.txt";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata { ContentType = "text/plain", SizeBytes = 100, LastModified = DateTimeOffset.UtcNow });
        _s3ClientMock.Setup(x => x.GetPreSignedUrl("test-bucket", fullKey, It.IsAny<TimeSpan>()))
            .Returns("http://localhost:8333/test-bucket/test-key?signature=abc");

        // Act
        var result = await service.GetPublicUrlAsync("test.txt");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("test-bucket", result.Url);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GetPublicUrlAsync_NonExistingFile_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", $"{_userId}/missing.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync((S3ObjectMetadata?)null);

        // Act
        var result = await service.GetPublicUrlAsync("missing.txt");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicUrlAsync_WithPublicServiceUrl_ReplacesInternalUrl()
    {
        // Arrange
        var optionsWithPublicUrl = Options.Create(new StorageOptions
        {
            ServiceUrl = "http://localhost:8333",
            PublicServiceUrl = "https://files.example.com",
            AccessKey = "admin",
            SecretKey = "admin",
            DefaultBucket = "test-bucket"
        });

        var service = CreateService(optionsWithPublicUrl);
        var fullKey = $"{_userId}/test.txt";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata { ContentType = "text/plain", SizeBytes = 100, LastModified = DateTimeOffset.UtcNow });
        _s3ClientMock.Setup(x => x.GetPreSignedUrl("test-bucket", fullKey, It.IsAny<TimeSpan>()))
            .Returns("http://localhost:8333/test-bucket/test-key?signature=abc");

        // Act
        var result = await service.GetPublicUrlAsync("test.txt");

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("https://files.example.com", result.Url);
        Assert.DoesNotContain("localhost:8333", result.Url);
    }

    [Fact]
    public async Task GetPublicUrlAsync_WithCustomExpiry_UsesProvidedExpiry()
    {
        // Arrange
        var service = CreateService();
        var fullKey = $"{_userId}/test.txt";
        var customExpiry = TimeSpan.FromMinutes(30);

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata { ContentType = "text/plain", SizeBytes = 100, LastModified = DateTimeOffset.UtcNow });
        _s3ClientMock.Setup(x => x.GetPreSignedUrl("test-bucket", fullKey, customExpiry))
            .Returns("http://localhost:8333/test-bucket/test-key?signature=abc");

        // Act
        var result = await service.GetPublicUrlAsync("test.txt", customExpiry);

        // Assert
        Assert.NotNull(result);
        _s3ClientMock.Verify(x => x.GetPreSignedUrl("test-bucket", fullKey, customExpiry), Times.Once);
    }

    #endregion

    #region GetPreviewUrlAsync Tests (S3)

    [Fact]
    public async Task GetPreviewUrlAsync_ExistingFile_ReturnsUrlWithResizeParams()
    {
        // Arrange
        var service = CreateService();
        var fullKey = $"{_userId}/image.png";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata { ContentType = "image/png", SizeBytes = 1000, LastModified = DateTimeOffset.UtcNow });
        _s3ClientMock.Setup(x => x.GetPreSignedUrl("test-bucket", fullKey, It.IsAny<TimeSpan>()))
            .Returns("http://localhost:8333/test-bucket/image-key?signature=abc");

        // Act
        var result = await service.GetPreviewUrlAsync("image.png", width: 200, height: 150);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("width=200", result.Url);
        Assert.Contains("height=150", result.Url);
        Assert.Contains("mode=fit", result.Url);
    }

    [Fact]
    public async Task GetPreviewUrlAsync_WithOnlyWidth_ReturnsUrlWithWidthParam()
    {
        // Arrange
        var service = CreateService();
        var fullKey = $"{_userId}/image.png";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata { ContentType = "image/png", SizeBytes = 1000, LastModified = DateTimeOffset.UtcNow });
        _s3ClientMock.Setup(x => x.GetPreSignedUrl("test-bucket", fullKey, It.IsAny<TimeSpan>()))
            .Returns("http://localhost:8333/test-bucket/image-key?signature=abc");

        // Act
        var result = await service.GetPreviewUrlAsync("image.png", width: 300);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("width=300", result.Url);
        Assert.DoesNotContain("height=", result.Url);
        Assert.Contains("mode=fit", result.Url);
    }

    [Fact]
    public async Task GetPreviewUrlAsync_NonExistingFile_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", $"{_userId}/missing.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync((S3ObjectMetadata?)null);

        // Act
        var result = await service.GetPreviewUrlAsync("missing.png", width: 200);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Filesystem UploadAsync Tests

    [Fact]
    public async Task UploadAsync_Filesystem_NoKeyPrefix_WritesToFilesystem()
    {
        // Arrange
        var service = CreateService(CreateFsOptions());
        var content = new MemoryStream("hello world"u8.ToArray());
        var request = new UploadFileRequest
        {
            FileName = "test.txt",
            ContentType = "text/plain",
            Content = content
        };

        // Act
        var result = await service.UploadAsync(request);

        // Assert
        Assert.Equal("test.txt", result.FileName);
        Assert.Equal("text/plain", result.ContentType);
        Assert.Equal(11, result.SizeBytes);
        Assert.Equal($"{_userId}/test.txt", result.ObjectKey);

        var filePath = Path.Combine(GetUserDir(), "test.txt");
        Assert.True(File.Exists(filePath));
        Assert.Equal("hello world", await File.ReadAllTextAsync(filePath));

        _s3ClientMock.Verify(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_Filesystem_WithKeyPrefix_UsesS3()
    {
        // Arrange
        var service = CreateService(CreateFsOptions());
        _s3ClientMock.Setup(x => x.BucketExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _s3ClientMock.Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var content = new MemoryStream("image data"u8.ToArray());
        var request = new UploadFileRequest
        {
            FileName = "photo.png",
            ContentType = "image/png",
            Content = content,
            KeyPrefix = "conversations/abc"
        };

        // Act
        var result = await service.UploadAsync(request);

        // Assert
        Assert.Equal($"{_userId}/conversations/abc/photo.png", result.ObjectKey);
        _s3ClientMock.Verify(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Filesystem ListAsync Tests

    [Fact]
    public async Task ListAsync_Filesystem_ReturnsFilesFromDirectory()
    {
        // Arrange
        var userDir = GetUserDir();
        Directory.CreateDirectory(userDir);
        await File.WriteAllTextAsync(Path.Combine(userDir, "file1.txt"), "content1");
        await File.WriteAllTextAsync(Path.Combine(userDir, "file2.png"), "content2");

        var service = CreateService(CreateFsOptions());

        // Act
        var result = await service.ListAsync();

        // Assert
        Assert.Equal(2, result.Count);
        var fileNames = result.Select(f => f.FileName).OrderBy(n => n).ToList();
        Assert.Contains("file1.txt", fileNames);
        Assert.Contains("file2.png", fileNames);

        _s3ClientMock.Verify(x => x.ListObjectsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListAsync_Filesystem_EmptyDirectory_ReturnsEmpty()
    {
        // Arrange
        var userDir = GetUserDir();
        Directory.CreateDirectory(userDir);

        var service = CreateService(CreateFsOptions());

        // Act
        var result = await service.ListAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAsync_Filesystem_MissingDirectory_ReturnsEmpty()
    {
        // Arrange (don't create the user directory)
        var service = CreateService(CreateFsOptions());

        // Act
        var result = await service.ListAsync();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Filesystem DownloadAsync Tests

    [Fact]
    public async Task DownloadAsync_Filesystem_ExistingUserFile_ReturnsFileContent()
    {
        // Arrange
        var userDir = GetUserDir();
        Directory.CreateDirectory(userDir);
        await File.WriteAllTextAsync(Path.Combine(userDir, "readme.txt"), "hello");

        var service = CreateService(CreateFsOptions());

        // Act
        var result = await service.DownloadAsync("readme.txt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("readme.txt", result.FileName);
        Assert.Equal("text/plain", result.ContentType);
        Assert.Equal(5, result.SizeBytes);

        using var reader = new StreamReader(result.Content);
        Assert.Equal("hello", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task DownloadAsync_Filesystem_NonExistingUserFile_ReturnsNull()
    {
        // Arrange
        var service = CreateService(CreateFsOptions());

        // Act
        var result = await service.DownloadAsync("missing.txt");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DownloadAsync_Filesystem_PathKeyedObject_UsesS3()
    {
        // Arrange
        var service = CreateService(CreateFsOptions());
        var fullKey = $"{_userId}/conversations/abc/image.png";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata { ContentType = "image/png", SizeBytes = 500, LastModified = DateTimeOffset.UtcNow });
        _s3ClientMock.Setup(x => x.DownloadAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream("img"u8.ToArray()));

        // Act
        var result = await service.DownloadAsync("conversations/abc/image.png");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("image.png", result.FileName);
        _s3ClientMock.Verify(x => x.GetObjectMetadataAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadAsync_Filesystem_TraversalAttempt_ReturnsNull()
    {
        // Arrange
        var userDir = GetUserDir();
        Directory.CreateDirectory(userDir);

        var service = CreateService(CreateFsOptions());

        // Act — ".." contains path traversal, but IsUserFile("..") would be true (no /)
        // The helper's validation catches it
        var result = await service.DownloadAsync("..");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Filesystem DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_Filesystem_ExistingUserFile_DeletesAndReturnsTrue()
    {
        // Arrange
        var userDir = GetUserDir();
        Directory.CreateDirectory(userDir);
        var filePath = Path.Combine(userDir, "to-delete.txt");
        await File.WriteAllTextAsync(filePath, "bye");

        var service = CreateService(CreateFsOptions());

        // Act
        var result = await service.DeleteAsync("to-delete.txt");

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteAsync_Filesystem_NonExistingUserFile_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(CreateFsOptions());

        // Act
        var result = await service.DeleteAsync("nope.txt");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_Filesystem_PathKeyedObject_UsesS3()
    {
        // Arrange
        var service = CreateService(CreateFsOptions());
        var fullKey = $"{_userId}/conversations/abc/image.png";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata { ContentType = "image/png", SizeBytes = 500, LastModified = DateTimeOffset.UtcNow });
        _s3ClientMock.Setup(x => x.DeleteAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await service.DeleteAsync("conversations/abc/image.png");

        // Assert
        Assert.True(result);
        _s3ClientMock.Verify(x => x.DeleteAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Filesystem GetPublicUrlAsync Tests

    [Fact]
    public async Task GetPublicUrlAsync_Filesystem_UserFile_ReturnsNull()
    {
        // Arrange
        var service = CreateService(CreateFsOptions());

        // Act
        var result = await service.GetPublicUrlAsync("test.txt");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicUrlAsync_Filesystem_PathKeyedObject_UsesS3()
    {
        // Arrange
        var service = CreateService(CreateFsOptions());
        var fullKey = $"{_userId}/conversations/abc/image.png";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync("test-bucket", fullKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata { ContentType = "image/png", SizeBytes = 500, LastModified = DateTimeOffset.UtcNow });
        _s3ClientMock.Setup(x => x.GetPreSignedUrl("test-bucket", fullKey, It.IsAny<TimeSpan>()))
            .Returns("http://localhost:8333/test-bucket/key?signature=abc");

        // Act
        var result = await service.GetPublicUrlAsync("conversations/abc/image.png");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("test-bucket", result.Url);
    }

    #endregion

    #region Filesystem GetPreviewUrlAsync Tests

    [Fact]
    public async Task GetPreviewUrlAsync_Filesystem_UserFile_ReturnsNull()
    {
        // Arrange
        var service = CreateService(CreateFsOptions());

        // Act
        var result = await service.GetPreviewUrlAsync("image.png", width: 200);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
