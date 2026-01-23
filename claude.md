Project is a Modular monolith.

## Project Structure

```
src/
├── DonkeyWork.Agents.Api/                     # Main API host
├── common/
│   ├── DonkeyWork.Agents.Common.Contracts/    # Shared enums, base interfaces
│   └── DonkeyWork.Agents.Persistence/         # Root persistence (single DbContext)
│       ├── Entities/BaseEntity.cs             # Base class (Id, UserId, CreatedAt, UpdatedAt)
│       ├── Entities/{Module}/                 # EF entities per module
│       ├── Configurations/{Module}/           # EF Fluent API configurations
│       ├── Repositories/{Module}/             # Repository implementations
│       └── Migrations/
├── {module}/
│   ├── DonkeyWork.Agents.{Module}.Contracts/  # Models, DTOs, service interfaces
│   ├── DonkeyWork.Agents.{Module}.Core/       # Service implementations
│   └── DonkeyWork.Agents.{Module}.Api/        # Controllers, DI registration
```

## Module Layers

- **Common.Contracts** => shared enums (e.g., LlmProvider, OAuthProvider), base interfaces (IEntity, IAuditable).
  - Referenced by all module Contracts and Persistence.
- **Contracts** => models (DTOs), service interfaces, module-specific enums for the module.
  - Models are clean classes for API/service layer use.
  - Referenced by other modules that need to interact with this module.
- **Core** => service implementations, business logic.
  - Not referenced by other modules.
  - Services work with models from Contracts, repositories handle entity mapping.
- **Api** => controllers, DI registration via `DependencyInjection.cs` with `Add{Module}Api()` extension method.
  - Not referenced by other modules.
- **Persistence** => single shared project containing entities, DbContext, EF configurations, repository implementations.
  - All entities inherit from `BaseEntity` (provides Id, UserId, CreatedAt, UpdatedAt).
  - Uses Fluent API for entity configuration (no EF attributes on entities).
  - Organized by module via folders (Entities/{Module}, Configurations/{Module}, Repositories/{Module}).
  - Repositories map between entities and models from Contracts.

## Infrastructure

- **Database:** PostgreSQL with pgcrypto and pgvector extensions
- **Message Broker:** RabbitMQ
- **Identity:** Keycloak

## Database

All databases use PostgreSQL with support for pgcrypto and pgvector extensions.

## EF Configuration Standards

- Table names: snake_case with module schema (e.g., `credentials.external_api_keys`)
- Primary keys: UUID with `gen_random_uuid()` default
- Enums: convert to strings via `HasConversion<string>()`
- Encrypted columns: use `bytea` column type
- No soft delete - use hard deletes
- Global query filter on `BaseEntity.UserId` for user isolation (use `IgnoreQueryFilters()` to bypass)

## Configuration

- Use `IOptions<T>` pattern for all configuration
- Options classes validate on startup via `ValidateDataAnnotations()` and `ValidateOnStart()`
- Development config in `appsettings.Development.json`
- Options classes live in `{Module}.Api/Options/` folder

Example:
```csharp
services.AddOptions<MyOptions>()
    .BindConfiguration("MySection")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## Controllers

- Use URL segment API versioning: `[Route("api/v{version:apiVersion}/[controller]")]`
- Apply `[ApiVersion(1.0)]` attribute to controllers
- Use Scalar for API documentation (via `app.MapScalarApiReference()`)
- Document all endpoints with:
  - XML comments for summary/description
  - `[ProducesResponseType<T>(StatusCodes.StatusXXX)]` for each response code
  - `[Produces("application/json")]` at controller level

## API Models

- All controllers must have dedicated request/response models
- Naming convention: `{MethodName}{Request|Response}V{Version}`
- Examples: `GetMeResponseV1`, `CreateUserRequestV1`, `CreateUserResponseV1`
- Models live in `{Module}.Contracts/Models/` folder

## Authentication

- Keycloak with JWT Bearer tokens
- Audience validated via `azp` claim (Keycloak sets `aud: "account"` by default)
- `IIdentityContext` provides authenticated user info (UserId, Email, Name, Username)
- UserId must be valid GUID from Keycloak `sub` claim
- See `keycloak.md` for configuration details

## Logging

- Serilog with format: `{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message}`
- Configure via `Serilog` section in appsettings
- `UseSerilogRequestLogging()` for HTTP request logs

## Code Standards

- One class per file (no multiple classes in a single file)
- File name must match class name
- Use `PaginationRequest` and `PaginatedResponse<T>` from `Common.Contracts.Models.Pagination` for paginated endpoints

## Unit Tests

- Test projects go under `test/{module}/{ProjectName}.Tests`
- Use Moq, not FluentAssertions
- Use xUnit, not NUnit
- Test method naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Test edge cases and error handling scenarios


