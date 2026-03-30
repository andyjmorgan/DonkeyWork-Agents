# DonkeyWork.Agents.Persistence

Shared persistence layer for the DonkeyWork.Agents modular monolith. Contains the single DbContext, EF configurations, interceptors, and repository implementations organized by module.

## Project Structure

```
DonkeyWork.Agents.Persistence/
├── AgentsDbContext.cs              # Single DbContext for all modules
├── AgentsDbContextFactory.cs       # Design-time factory for EF migrations
├── DependencyInjection.cs          # AddPersistence() extension method
├── PersistenceOptions.cs           # Configuration options
├── Configurations/
│   └── {Module}/                   # EF Fluent API configurations per module
├── Entities/
│   ├── BaseEntity.cs               # Base class with Id, UserId, audit timestamps
│   └── {Module}/                   # Entity classes per module
├── Interceptors/
│   └── AuditableInterceptor.cs     # Sets CreatedAt/UpdatedAt timestamps
├── Migrations/                     # EF Core migrations
├── Repositories/
│   └── {Module}/                   # Repository implementations per module
└── Services/
    └── MigrationService.cs         # Programmatic migration service
```

## Database

PostgreSQL with support for:
- **pgcrypto** - Column-level encryption for sensitive data
- **pgvector** - Vector similarity search (future)

## DbContext

`AgentsDbContext` is the single shared DbContext for all modules. It:
- Exposes DbSets for all entities
- Auto-discovers configurations via `ApplyConfigurationsFromAssembly()`
- Applies global query filter on all `BaseEntity` types to filter by `UserId`

### User Isolation (Query Filter)

All entities inheriting from `BaseEntity` automatically have a query filter applied:

```csharp
entity.HasQueryFilter(e => e.UserId == CurrentUserId);
```

This ensures users can only access their own data. The `CurrentUserId` is derived from `IIdentityContext` (populated from JWT auth).

To bypass the filter (admin scenarios):

```csharp
dbContext.ExternalApiKeys.IgnoreQueryFilters().ToList();
```

## Interceptors

### AuditableInterceptor

Handles `IAuditable` entities automatically:
- **Added entities**: Sets both `CreatedAt` and `UpdatedAt` to `DateTimeOffset.UtcNow`
- **Modified entities**: Updates `UpdatedAt` to `DateTimeOffset.UtcNow`

## Entities

All entities inherit from `BaseEntity` which provides:
- `Id` - GUID primary key
- `UserId` - GUID identifying the owning user
- `CreatedAt`, `UpdatedAt` - Audit timestamps (from `IAuditable`)

### Credentials Module

| Entity | Description |
|--------|-------------|
| `ExternalApiKeyEntity` | Third-party LLM provider API keys (OpenAI, Anthropic, etc.) |
| `OAuthTokenEntity` | OAuth tokens for connected services |
| `OAuthStateEntity` | OAuth flow state for CSRF protection |
| `OAuthProviderConfigEntity` | Custom OAuth app configurations |
| `UserApiKeyEntity` | User-generated API keys for accessing this system |
| `SandboxCredentialMappingEntity` | Mappings between credentials and sandbox environments |
| `SandboxCustomVariableEntity` | Custom environment variables for sandboxes |

## Entity Configurations

Configurations use EF Core Fluent API (no data annotations on entities):
- Table names use snake_case with module schema prefix (e.g., `credentials.external_api_keys`)
- Primary keys use `gen_random_uuid()` for UUID generation
- Enums convert to strings in the database
- Encrypted columns use `bytea` column type
- Appropriate indexes for common query patterns

## Configuration

Add to `appsettings.json`:

```json
{
  "Persistence": {
    "ConnectionString": "Host=localhost;Database=donkeywork_agents;Username=postgres;Password=postgres",
    "EncryptionKey": "your-encryption-key-here"
  }
}
```

## DI Registration

In your API project's `Program.cs`:

```csharp
builder.Services.AddPersistence(builder.Configuration);
```

This registers:
- `AgentsDbContext` with Npgsql, retry policy, and interceptors
- `AuditableInterceptor` (Singleton)
- `IMigrationService` (Scoped)

## Migrations

### Running Migrations at Startup (Optional)

```csharp
// In Program.cs
using var scope = app.Services.CreateScope();
var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
await migrationService.MigrateAsync();
```

### Creating Migrations

```bash
dotnet ef migrations add <MigrationName> \
  --startup-project src/DonkeyWork.Agents.Api \
  --project src/common/DonkeyWork.Agents.Persistence
```

### Applying Migrations

```bash
dotnet ef database update \
  --startup-project src/DonkeyWork.Agents.Api \
  --project src/common/DonkeyWork.Agents.Persistence
```

## Adding a New Module

1. **Create entity classes** in `Entities/{Module}/`
   - Inherit from `BaseEntity`

2. **Create configurations** in `Configurations/{Module}/`
   - Implement `IEntityTypeConfiguration<TEntity>`
   - Use Fluent API for all configuration
   - Configure base properties (Id, UserId, CreatedAt)

3. **Add DbSets** to `AgentsDbContext.cs`

4. **Create repositories** in `Repositories/{Module}/` (optional)

5. **Generate migration**:
   ```bash
   dotnet ef migrations add Add{Module}Entities \
     --startup-project src/DonkeyWork.Agents.Api \
     --project src/common/DonkeyWork.Agents.Persistence
   ```

## Example Entity

```csharp
public class MyEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    // Add entity-specific properties here
    // BaseEntity provides: Id, UserId, CreatedAt, UpdatedAt
}
```

## Example Configuration

```csharp
public class MyEntityConfiguration : IEntityTypeConfiguration<MyEntity>
{
    public void Configure(EntityTypeBuilder<MyEntity> builder)
    {
        builder.ToTable("my_entities", "mymodule");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.UserId);
    }
}
```
