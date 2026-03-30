# Storage Module

Blob storage module for the DonkeyWork-Agents modular monolith using **SeaweedFS** as the S3-compatible storage backend. There is no PostgreSQL metadata layer -- S3 is the single source of truth for file data. An optional filesystem-backed mode is available for user files.

## Module Structure

```
src/storage/
├── DonkeyWork.Agents.Storage.Contracts/   # Models, DTOs, service interfaces
├── DonkeyWork.Agents.Storage.Core/        # Service implementations, options
└── DonkeyWork.Agents.Storage.Api/         # Controllers, DI registration
```

## Storage Backend

**SeaweedFS** (Apache 2.0 license)
- S3-compatible API with presigned URL support
- Lightweight, suitable for dev and small-scale production
- Docker: `docker run -d -p 9333:9333 -p 8333:8333 chrislusf/seaweedfs server -s3`

## Object Key Scheme

- **User files**: `{userId}/{filename}` (flat per-user namespace)
- **Conversation images**: `{userId}/conversations/{convId}/{filename}`
- File identifier is the filename, not a UUID
- Uploading the same filename overwrites the existing file
- Hard delete only

## API Endpoints

### Files (`/api/v1/files`)
- `GET /` - List files and folders for the current user
- `POST /` - Upload file (multipart/form-data, 100MB limit)
- `GET /{filename}/download` - Download by filename
- `GET /{filename}/url` - Presigned URL by filename
- `DELETE /{filename}` - Delete by filename
- `GET /download/{**key}` - Download by path key (conversation images)
- `GET /url/{**key}` - Presigned URL by path key

## Configuration

Add to `appsettings.json`:

```json
{
  "Storage": {
    "ServiceUrl": "http://localhost:8333",
    "AccessKey": "admin",
    "SecretKey": "admin",
    "DefaultBucket": "files",
    "UsePathStyleAddressing": true,
    "PublicServiceUrl": null,
    "FileSystemBasePath": null,
    "UserFilesSubPath": "files",
    "SkillsSubPath": "skills"
  }
}
```

When `FileSystemBasePath` is set, user files are stored on the filesystem instead of S3.

## Registration

Register the module in `Program.cs`:

```csharp
builder.Services.AddStorageApi(builder.Configuration);
```

## Testing

Run unit tests:

```bash
dotnet test test/storage/DonkeyWork.Agents.Storage.Tests
```
