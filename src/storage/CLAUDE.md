# Storage Module - Development Instructions

## Module Overview

This is a blob storage module following the modular monolith architecture. It uses SeaweedFS (S3-compatible) as the sole storage backend — there is no PostgreSQL metadata layer. S3 is the single source of truth for all file data.

## Object Key Scheme

- **User files**: `{userId}/{filename}` (flat per-user namespace)
- **Conversation images**: `{userId}/conversations/{convId}/{filename}`
- File identifier is the filename, not a UUID
- Uploading the same filename overwrites the existing file
- Hard delete only — no soft delete or grace period

## Project Responsibilities

### Contracts (`DonkeyWork.Agents.Storage.Contracts`)
- Models: `UploadFileRequest`, `StorageUploadResult`, `FileItemV1`, `FileDownloadResult`, `PresignedUrlResult`, `S3ObjectInfo`, `S3ObjectMetadata`
- Service interfaces: `IStorageService`, `IS3ClientWrapper`

### Core (`DonkeyWork.Agents.Storage.Core`)
- `StorageOptions` options class (ServiceUrl, PublicServiceUrl, AccessKey, SecretKey, DefaultBucket)
- `IS3ClientWrapper` / `S3ClientWrapper` for testable S3 abstraction
- `StorageService` — file upload, download, list, delete, presigned URLs (all S3-native)

### Api (`DonkeyWork.Agents.Storage.Api`)
- `FilesController` — filename-based file endpoints
- `DependencyInjection.cs` with `AddStorageApi()` extension method

## Key Implementation Details

### S3 Client Wrapper
Wraps the AWS SDK S3 client for testability:
```csharp
public interface IS3ClientWrapper
{
    Task<bool> BucketExistsAsync(string bucket, CancellationToken ct);
    Task CreateBucketAsync(string bucket, CancellationToken ct);
    Task UploadAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct);
    Task<Stream> DownloadAsync(string bucket, string key, CancellationToken ct);
    Task DeleteAsync(string bucket, string key, CancellationToken ct);
    Task<List<S3ObjectInfo>> ListObjectsAsync(string bucket, string prefix, string? delimiter, CancellationToken ct);
    Task<S3ObjectMetadata?> GetObjectMetadataAsync(string bucket, string key, CancellationToken ct);
    Task DeleteByPrefixAsync(string bucket, string prefix, CancellationToken ct);
    string GetPreSignedUrl(string bucket, string key, TimeSpan expiry);
}
```

### StorageService
All methods prepend `{userId}/` from `IIdentityContext` to keys, ensuring user isolation at the S3 key level:
- `UploadAsync` — builds key as `{userId}/{keyPrefix}/{filename}` or `{userId}/{filename}`, auto-creates bucket if needed
- `ListAsync` — uses S3 `ListObjectsV2` with delimiter `/` for top-level files only
- `DownloadAsync` — uses HeadObject for metadata, then GetObject for content
- `DeleteAsync` — checks existence via HeadObject, then deletes
- `DeleteByPrefixAsync` — lists and batch-deletes all objects under a prefix (used for conversation cleanup)
- `GetPublicUrlAsync` — generates presigned URL, optionally replaces internal URL with `PublicServiceUrl`
- `GetPreviewUrlAsync` — generates presigned URL with resize query parameters

### API Routes (filename-based)
```
GET    /api/v1/files                        → list all user files
POST   /api/v1/files                        → upload file
GET    /api/v1/files/{filename}/download     → download by filename
GET    /api/v1/files/{filename}/url          → presigned URL by filename
DELETE /api/v1/files/{filename}              → delete by filename
GET    /api/v1/files/download/{**key}        → download by path key (conversation images)
GET    /api/v1/files/url/{**key}             → presigned URL by path key
```

## Testing Guidelines

- Use Moq for mocking `IS3ClientWrapper`
- Use xUnit for test framework
- Test method naming: `MethodName_StateUnderTest_ExpectedBehavior`
- No database dependencies — all tests mock the S3 client wrapper

Example test structure:
```csharp
public class StorageServiceTests
{
    private readonly Mock<IS3ClientWrapper> _s3ClientMock;
    private readonly Mock<IIdentityContext> _identityContextMock;
    private readonly IOptions<StorageOptions> _options;

    [Fact]
    public async Task UploadAsync_ValidFile_ReturnsUploadResult()
    {
        // Arrange
        _s3ClientMock.Setup(x => x.BucketExistsAsync(...)).ReturnsAsync(true);
        _s3ClientMock.Setup(x => x.UploadAsync(...)).Returns(Task.CompletedTask);

        // Act
        var result = await service.UploadAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.txt", result.FileName);
    }
}
```

## Common Patterns

### Controller Actions
Return appropriate status codes:
- `200 OK` for successful GET (list, download, URL generation)
- `201 Created` for successful POST (upload)
- `204 No Content` for successful DELETE
- `404 Not Found` when file doesn't exist

### Configuration Binding
```csharp
services.AddOptions<StorageOptions>()
    .BindConfiguration("Storage")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```
