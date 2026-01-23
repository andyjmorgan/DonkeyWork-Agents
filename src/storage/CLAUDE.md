# Storage Module - Development Instructions

## Module Overview

This is a blob storage module following the modular monolith architecture. It uses SeaweedFS (S3-compatible) for file storage and PostgreSQL for metadata.

## Project Responsibilities

### Contracts (`DonkeyWork.Agents.Storage.Contracts`)
- Entities: `StoredFile`, `FileShare` (live here so services can work with them directly)
- Enums: `FileStatus`, `ShareStatus`
- Models: `StoredFileModel`, `FileShareModel`, `UploadFileRequest`, `CreateShareRequest`, `PresignedUrlResult`
- Repository interfaces: `IStoredFileRepository`, `IFileShareRepository`
- Service interfaces: `IStorageService`, `IFileShareService`, `IStorageCleanupService`

### Core (`DonkeyWork.Agents.Storage.Core`)
- `StorageConfiguration` options class
- `IS3ClientWrapper` / `S3ClientWrapper` for testable S3 abstraction
- `StorageService` - file upload, download, metadata operations
- `FileShareService` - share link creation, validation, download
- `StorageCleanupService` - cleanup expired shares and soft-deleted files
- `StorageCleanupBackgroundService` - hourly cleanup hosted service

### Api (`DonkeyWork.Agents.Storage.Api`)
- `FilesController` - file CRUD endpoints
- `SharesController` - share link endpoints
- `DependencyInjection.cs` with `AddStorageApi()` extension method

### Shared Persistence (`src/common/DonkeyWork.Agents.Persistence`)
Storage-related persistence lives in the shared persistence project:
- `Configurations/Storage/` - EF Fluent API configurations for `StoredFile`, `FileShare`
- `Repositories/Storage/` - Repository implementations: `StoredFileRepository`, `FileShareRepository`

## Key Implementation Details

### Entity Configuration
Entities live in Contracts (no EF attributes). Use Fluent API in Persistence:
```csharp
// src/common/DonkeyWork.Agents.Persistence/Configurations/Storage/StoredFileConfiguration.cs
public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("stored_files");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ObjectKey).HasMaxLength(1024).IsRequired();
        builder.HasIndex(x => x.ObjectKey).IsUnique();
        // ...
    }
}
```

### S3 Client Wrapper
Wrap the AWS S3 client in an interface for testability:
```csharp
public interface IS3ClientWrapper
{
    Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken ct = default);
    Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request, CancellationToken ct = default);
    Task<DeleteObjectResponse> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken ct = default);
    string GetPreSignedURL(GetPreSignedUrlRequest request);
}
```

### Share Token Generation
Use cryptographically secure random tokens:
```csharp
using var rng = RandomNumberGenerator.Create();
var bytes = new byte[32];
rng.GetBytes(bytes);
return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
```

### Password Hashing
Use BCrypt for optional share passwords:
```csharp
BCrypt.Net.BCrypt.HashPassword(password)
BCrypt.Net.BCrypt.Verify(password, hash)
```

### Soft Delete Pattern
Files use a three-state deletion:
1. `Active` - normal state
2. `MarkedForDeletion` - soft deleted, can be restored within grace period
3. `Deleted` - hard deleted, S3 object removed

### Background Cleanup
The `StorageCleanupBackgroundService` runs hourly to:
1. Mark expired shares as `Expired`
2. Hard delete files past the grace period (remove from S3, update status)

## Testing Guidelines

- Use Moq for mocking dependencies
- Use xUnit for test framework
- Test method naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Mock the `IS3ClientWrapper` in service tests
- Use in-memory database for repository tests if needed

Example test structure:
```csharp
public class StorageServiceTests
{
    [Fact]
    public async Task UploadFileAsync_ValidFile_ReturnsStoredFileModel()
    {
        // Arrange
        var mockS3Client = new Mock<IS3ClientWrapper>();
        var mockRepository = new Mock<IStoredFileRepository>();
        // ...

        // Act
        var result = await service.UploadFileAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedFileName, result.FileName);
    }
}
```

## Common Patterns

### Controller Actions
Return appropriate status codes:
- `200 OK` for successful GET
- `201 Created` for successful POST with Location header
- `204 No Content` for successful DELETE
- `404 Not Found` when resource doesn't exist
- `400 Bad Request` for validation failures
- `401 Unauthorized` for invalid share passwords

### Repository Pattern
Repositories should:
- Return entities from Contracts
- Services handle mapping to models at API boundary
- Handle null cases gracefully
- Use async/await throughout
- Include cancellation token support

### Configuration Binding
```csharp
services.Configure<StorageConfiguration>(configuration.GetSection("Storage"));
```
