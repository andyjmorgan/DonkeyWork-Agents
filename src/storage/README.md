# Storage Module

Blob storage module for the DonkeyWork-Agents modular monolith using **SeaweedFS** as the S3-compatible storage backend with PostgreSQL for metadata tracking.

## Module Structure

```
src/storage/
├── DonkeyWork.Agents.Storage.Contracts/   # Entities, Interfaces, DTOs, Enums
├── DonkeyWork.Agents.Storage.Core/        # Service implementations
└── DonkeyWork.Agents.Storage.Api/         # Controllers, DI registration

src/common/DonkeyWork.Agents.Persistence/
├── Configurations/Storage/                # EF Fluent API configurations
└── Repositories/Storage/                  # Repository implementations
```

## Storage Backend

**SeaweedFS** (Apache 2.0 license)
- S3-compatible API with presigned URL support
- Lightweight, suitable for dev and small-scale production
- Docker: `docker run -d -p 9333:9333 -p 8333:8333 chrislusf/seaweedfs server -s3`

## Database Entities

Entities are defined in `Storage.Contracts` with EF configuration via Fluent API in the shared Persistence project.

### StoredFile
Tracks file metadata and S3 object references.

| Column | Type | Notes |
|--------|------|-------|
| id | UUID | PK, gen_random_uuid() |
| file_name | VARCHAR(500) | Original filename |
| content_type | VARCHAR(255) | MIME type |
| size_bytes | BIGINT | File size |
| bucket_name | VARCHAR(255) | S3 bucket |
| object_key | VARCHAR(1024) | S3 key, unique |
| checksum_sha256 | VARCHAR(64) | Integrity check |
| status | INT | Active/MarkedForDeletion/Deleted |
| created_at_utc | TIMESTAMPTZ | |
| marked_for_deletion_at_utc | TIMESTAMPTZ | Soft delete timestamp |
| deleted_at_utc | TIMESTAMPTZ | Hard delete timestamp |
| owner_id | UUID | Optional owner reference |
| metadata | JSONB | Flexible attributes |

### FileShare
Manages shareable links with expiration and optional password protection.

| Column | Type | Notes |
|--------|------|-------|
| id | UUID | PK |
| file_id | UUID | FK → stored_files |
| share_token | VARCHAR(128) | Unique, cryptographic |
| expires_at_utc | TIMESTAMPTZ | Default: 24 hours |
| status | INT | Active/Expired/Revoked |
| created_at_utc | TIMESTAMPTZ | |
| max_downloads | INT | Optional limit |
| download_count | INT | |
| password_hash | VARCHAR(255) | BCrypt, optional |

## API Endpoints

### Files (`/api/storage/files`)
- `POST /` - Upload file (multipart/form-data)
- `GET /{id}` - Get file metadata
- `GET /{id}/download` - Download file
- `GET ?ownerId={guid}` - List files by owner
- `DELETE /{id}` - Soft delete file

### Shares (`/api/storage/shares`)
- `POST /` - Create share link (returns presigned URL)
- `GET /{token}` - Get share metadata
- `GET /{token}/download` - Download via share (anonymous)
- `GET /file/{fileId}` - List shares for file
- `DELETE /{shareId}` - Revoke share

## Configuration

Add to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "StorageDb": "Host=localhost;Database=donkeywork_storage;Username=postgres;Password=postgres"
  },
  "Storage": {
    "ServiceUrl": "http://localhost:8333",
    "AccessKey": "admin",
    "SecretKey": "admin",
    "DefaultBucket": "files",
    "DefaultShareExpiry": "1.00:00:00",
    "FileDeletionGracePeriod": "30.00:00:00",
    "UsePathStyleAddressing": true
  }
}
```

## Docker Compose

Add SeaweedFS to your docker-compose.yml:

```yaml
seaweedfs:
  image: chrislusf/seaweedfs
  command: server -s3
  ports:
    - "9333:9333"  # Master
    - "8333:8333"  # S3 API
  volumes:
    - seaweedfs_data:/data
```

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
