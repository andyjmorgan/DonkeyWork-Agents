# Data Protection Key Storage in PostgreSQL

## Problem Statement

MCP servers in this architecture use the `--stateless` flag, which means multiple instances can be spawned dynamically. ASP.NET Core's Data Protection system is used to protect sensitive data like authentication cookies, anti-forgery tokens, and other cryptographic operations.

By default, Data Protection keys are stored in memory or on the local file system, which causes problems in a multi-instance environment:

1. **Key Isolation**: Each instance generates its own keys, so data protected by one instance cannot be unprotected by another
2. **Key Loss**: When an instance restarts, in-memory keys are lost, invalidating all previously protected data
3. **Inconsistent State**: Users may experience authentication failures when load-balanced to different instances

To enable stateless operation across multiple MCP server instances, all instances must share the same Data Protection key ring stored in a central, persistent location.

## Solution Overview

ASP.NET Core provides the `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` package to persist Data Protection keys to a database using Entity Framework Core. This approach:

- Stores keys in a PostgreSQL table accessible by all instances
- Automatically handles key rotation and expiration
- Integrates with existing EF Core infrastructure
- Supports multiple applications sharing the same key ring via `SetApplicationName`

## Implementation Options

There are two approaches to implementing this solution:

### Option A: Extend Existing AgentsDbContext (Recommended)

Add the `IDataProtectionKeyContext` interface to the existing `AgentsDbContext`. This approach:
- Uses a single DbContext for all operations
- Requires special handling for the global query filter (DataProtectionKey entity is system-level, not user-scoped)
- Keeps all database operations in one place

### Option B: Separate DataProtectionDbContext

Create a dedicated `DataProtectionDbContext` specifically for key storage. This approach:
- Provides complete isolation from application concerns
- Avoids any complications with global query filters
- Follows Microsoft's examples more closely
- Adds slight complexity with multiple DbContexts

**Recommendation**: Option A is preferred for this project because it maintains the single-DbContext pattern established in the codebase. The global query filter concern is easily addressed since `DataProtectionKey` does not inherit from `BaseEntity`.

## Required NuGet Package

Add to the API project (`DonkeyWork.Agents.Api.csproj`):

```xml
<PackageReference Include="Microsoft.AspNetCore.DataProtection.EntityFrameworkCore" Version="10.0.1" />
```

## Implementation Steps

### Step 1: Modify AgentsDbContext

Update `AgentsDbContext` to implement `IDataProtectionKeyContext`:

```csharp
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Persistence;

public class AgentsDbContext : DbContext, IDataProtectionKeyContext
{
    // ... existing code ...

    // Data Protection keys (system-level, no user scoping)
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    // ... rest of existing code ...
}
```

### Step 2: Add Entity Configuration

Create a new configuration file at `src/common/DonkeyWork.Agents.Persistence/Configurations/System/DataProtectionKeyConfiguration.cs`:

```csharp
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DonkeyWork.Agents.Persistence.Configurations.System;

public class DataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
    public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
    {
        // Use a dedicated schema for system-level tables
        builder.ToTable("data_protection_keys", "system");

        // DataProtectionKey has an int Id by default, keep it
        builder.HasKey(k => k.Id);

        // The Xml column stores the serialized key data
        builder.Property(k => k.Xml)
            .HasColumnType("text");

        builder.Property(k => k.FriendlyName)
            .HasMaxLength(500);
    }
}
```

### Step 3: Verify No Global Query Filter Applied

The existing `AgentsDbContext.OnModelCreating` only applies the global query filter to entities that inherit from `BaseEntity`:

```csharp
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
    {
        // Filter applied only to BaseEntity descendants
    }
}
```

Since `DataProtectionKey` is from the Microsoft package and does NOT inherit from `BaseEntity`, it will automatically be excluded from the global query filter. No additional changes are needed.

### Step 4: Create Migration

Generate a migration for the new table:

```bash
dotnet ef migrations add AddDataProtectionKeys \
    --project src/common/DonkeyWork.Agents.Persistence \
    --startup-project src/DonkeyWork.Agents.Api \
    --context AgentsDbContext
```

The migration will create the `system.data_protection_keys` table with the following schema:

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | integer | No (PK) | Auto-incrementing primary key |
| FriendlyName | varchar(500) | Yes | Human-readable key identifier |
| Xml | text | Yes | Serialized XML key data |

### Step 5: Configure Data Protection in Program.cs

Add Data Protection configuration in the API's `Program.cs`:

```csharp
using Microsoft.AspNetCore.DataProtection;

// ... existing service configuration ...

builder.Services.AddDataProtection()
    .SetApplicationName("DonkeyWork.Agents")  // Required for key sharing
    .PersistKeysToDbContext<AgentsDbContext>();
```

### Step 6: Configure Key Lifetime (Optional)

The default key lifetime is 90 days. To customize:

```csharp
builder.Services.AddDataProtection()
    .SetApplicationName("DonkeyWork.Agents")
    .PersistKeysToDbContext<AgentsDbContext>()
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));
```

## Key Rotation Considerations

### Automatic Key Rotation

Data Protection automatically handles key rotation:
- New keys are generated before the current key expires
- Old keys are retained to decrypt existing protected data
- The key ring is automatically refreshed from the database

### Multi-Instance Behavior

When multiple instances share the same database:
1. One instance creates a new key when needed
2. Other instances automatically pick up the new key on their next key ring refresh
3. All instances can decrypt data protected by any instance

### Key Ring Refresh

The key ring is refreshed:
- On application startup
- Periodically (default: every 24 hours)
- When a decryption operation fails with the current key ring

## Security Considerations

### Keys Stored in Plain XML

By default, `PersistKeysToDbContext` stores keys as plain XML in the database. The keys contain the cryptographic material needed to protect/unprotect data.

For production deployments, consider encrypting keys at rest using one of these options:

#### Option 1: Certificate-Based Encryption

```csharp
builder.Services.AddDataProtection()
    .SetApplicationName("DonkeyWork.Agents")
    .PersistKeysToDbContext<AgentsDbContext>()
    .ProtectKeysWithCertificate(
        new X509Certificate2("path/to/certificate.pfx", "password"));
```

#### Option 2: Azure Key Vault (for Azure deployments)

```csharp
builder.Services.AddDataProtection()
    .SetApplicationName("DonkeyWork.Agents")
    .PersistKeysToDbContext<AgentsDbContext>()
    .ProtectKeysWithAzureKeyVault(
        new Uri("https://your-vault.vault.azure.net/keys/dataprotection"),
        new DefaultAzureCredential());
```

Requires packages:
- `Azure.Extensions.AspNetCore.DataProtection.Keys`
- `Azure.Identity`

#### Option 3: Windows DPAPI (Windows-only)

```csharp
builder.Services.AddDataProtection()
    .SetApplicationName("DonkeyWork.Agents")
    .PersistKeysToDbContext<AgentsDbContext>()
    .ProtectKeysWithDpapi();
```

### Database Security

Since keys are stored in the database:
- Ensure database connections use TLS/SSL
- Restrict database user permissions to only the required tables
- Consider using a dedicated database user for the application
- Enable PostgreSQL audit logging for the `system.data_protection_keys` table

### Application Name Isolation

The `SetApplicationName` call is critical:
- All instances that should share keys MUST use the same application name
- Different applications using the same database should use different application names
- This provides logical isolation even when sharing the same key storage table

## Complete Configuration Example

```csharp
// Program.cs

var builder = WebApplication.CreateBuilder(args);

// ... existing service configuration ...

// Configure Data Protection with PostgreSQL storage
builder.Services.AddDataProtection()
    .SetApplicationName("DonkeyWork.Agents")
    .PersistKeysToDbContext<AgentsDbContext>()
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// For production with certificate encryption:
// var cert = new X509Certificate2(
//     builder.Configuration["DataProtection:CertificatePath"],
//     builder.Configuration["DataProtection:CertificatePassword"]);
//
// builder.Services.AddDataProtection()
//     .SetApplicationName("DonkeyWork.Agents")
//     .PersistKeysToDbContext<AgentsDbContext>()
//     .ProtectKeysWithCertificate(cert);

var app = builder.Build();

// ... rest of application ...
```

## Testing Considerations

### Integration Tests

When running integration tests with TestContainers:
- The Data Protection keys table will be created via migrations
- Each test run gets a fresh database, so keys won't persist between test runs
- This is acceptable for testing since authentication state doesn't need to persist

### Unit Tests

For unit tests using the in-memory database:
- `DataProtectionKey` entity will be available in the in-memory provider
- No special configuration needed since the entity doesn't have a global query filter

## References

- [Microsoft Docs: Key Storage Providers](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers)
- [Microsoft Docs: Configure Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview)
- [Microsoft Docs: Key Encryption at Rest](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-encryption-at-rest)
- [NuGet: Microsoft.AspNetCore.DataProtection.EntityFrameworkCore](https://www.nuget.org/packages/Microsoft.AspNetCore.DataProtection.EntityFrameworkCore/)
- [Andrew Lock: Introduction to Data Protection](https://andrewlock.net/an-introduction-to-the-data-protection-system-in-asp-net-core/)
