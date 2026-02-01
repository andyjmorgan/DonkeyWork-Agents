Project is a Modular monolith.

## Prerequisites

- **.NET 10 SDK** is required to build and test this project
- Install via script: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0`
- After installation, add to PATH: `export PATH="$HOME/.dotnet:$PATH"`

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
- One file per class, no more. this applies to all classes in the project.

## Authentication

- Keycloak with JWT Bearer tokens
- Audience validated via `azp` claim (Keycloak sets `aud: "account"` by default)
- See `keycloak.md` for configuration details

### IIdentityContext

Scoped service providing authenticated user info throughout request lifetime.

**Interface** (`Identity.Contracts/Services/IIdentityContext.cs`):
```csharp
public interface IIdentityContext
{
    Guid UserId { get; }        // From Keycloak 'sub' claim (must be valid GUID)
    string? Email { get; }      // From 'email' claim
    string? Name { get; }       // From 'name' claim
    string? Username { get; }   // From 'preferred_username' claim
    bool IsAuthenticated { get; }
}
```

**Population**: Set automatically via JWT `OnTokenValidated` event or API Key auth handler.

**Usage in services**:
```csharp
public class MyService(IIdentityContext identityContext)
{
    public async Task DoSomething()
    {
        var userId = identityContext.UserId;  // Current authenticated user
    }
}
```

**Usage in DbContext**: Injected into `AgentsDbContext` for automatic user filtering:
```csharp
// DbContext exposes CurrentUserId from IIdentityContext
public Guid CurrentUserId => _identityContext?.UserId ?? Guid.Empty;

// Global query filter applied to all BaseEntity types
modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.UserId == CurrentUserId);
```

**Key points**:
- UserId must be valid GUID from `sub` claim - authentication fails if not parseable
- All entities inherit `BaseEntity.UserId` and are automatically filtered by current user
- Use `IgnoreQueryFilters()` to bypass user isolation (e.g., for admin/system operations)

## Service Design Pattern

**IMPORTANT**: Services must use `IIdentityContext` internally, NOT accept `userId` parameters.

**Correct pattern**:
```csharp
// Interface - no userId parameter
public interface IProjectService
{
    Task<ProjectDetailsV1> CreateAsync(CreateProjectRequestV1 request, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectSummaryV1>> ListAsync(CancellationToken ct = default);
}

// Implementation - inject IIdentityContext
public class ProjectService : IProjectService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;

    public ProjectService(AgentsDbContext dbContext, IIdentityContext identityContext, ...) { ... }

    public async Task<ProjectDetailsV1> CreateAsync(CreateProjectRequestV1 request, CancellationToken ct)
    {
        var userId = _identityContext.UserId;  // Get user internally
        // ...
    }
}
```

**Incorrect pattern** (DO NOT USE):
```csharp
// BAD: Passing userId as parameter
Task<ProjectDetailsV1> CreateAsync(CreateProjectRequestV1 request, Guid userId, CancellationToken ct);
```

**Rationale**:
- DbContext global query filters use `IIdentityContext.UserId` for user isolation
- Passing userId parameter creates mismatch between filter and service logic
- Controllers shouldn't need to extract/pass userId - services handle it
- Consistent with authentication architecture throughout the project

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
- Organize tests with `#region MethodName Tests` / `#endregion`

### MockDbContext Helper

Create a `Helpers/MockDbContext.cs` in each test project for in-memory database setup:

```csharp
public static class MockDbContext
{
    /// <summary>
    /// Creates DbContext with mocked IIdentityContext for user isolation.
    /// </summary>
    public static (AgentsDbContext DbContext, IIdentityContext IdentityContext) CreateWithIdentityContext(
        string? databaseName = null,
        Guid? userId = null)
    {
        databaseName ??= Guid.NewGuid().ToString();  // Unique DB per test
        userId ??= Guid.Parse("11111111-1111-1111-1111-111111111111");

        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var mockIdentityContext = new Mock<IIdentityContext>();
        mockIdentityContext.Setup(x => x.UserId).Returns(userId.Value);
        mockIdentityContext.Setup(x => x.Email).Returns("test@example.com");
        mockIdentityContext.Setup(x => x.Name).Returns("Test User");
        mockIdentityContext.Setup(x => x.Username).Returns("testuser");

        var context = new AgentsDbContext(options, mockIdentityContext.Object);
        return (context, mockIdentityContext.Object);
    }

    // Add Seed methods for entities: SeedProject(), SeedMilestone(), etc.
}
```

### Service Test Pattern

```csharp
public class ProjectServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly ProjectService _service;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public ProjectServiceTests()
    {
        // Use same IIdentityContext for both DbContext and service
        (_dbContext, _identityContext) = MockDbContext.CreateWithIdentityContext();
        var logger = new Mock<ILogger<ProjectService>>();
        _service = new ProjectService(_dbContext, _identityContext, logger.Object);
    }

    public void Dispose() => _dbContext?.Dispose();

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesProject()
    {
        // Arrange
        var request = new CreateProjectRequestV1 { Name = "test" };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
    }

    #endregion
}
```

### Controller Test Pattern

```csharp
public class AuthControllerTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly IOptions<KeycloakOptions> _keycloakOptions;

    public AuthControllerTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _keycloakOptions = Options.Create(new KeycloakOptions { ... });
    }

    private AuthController CreateController(HttpContext? httpContext = null)
    {
        var controller = new AuthController(_keycloakOptions, _httpClientFactoryMock.Object);
        if (httpContext != null)
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public void Login_RedirectsToKeycloak()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        var controller = CreateController(httpContext);

        // Act
        var result = controller.Login();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Contains("client_id=", redirectResult.Url);
    }
}
```

### Key Points

- **User isolation**: DbContext global query filter uses `IIdentityContext.UserId` - test at integration level, not unit tests
- **Same identity context**: Pass the same `IIdentityContext` mock to both DbContext and service
- **Unique database names**: Use `Guid.NewGuid().ToString()` for database isolation between tests
- **IDisposable**: Implement for cleanup of DbContext

## Integration Tests

Integration tests use TestContainers to spin up real PostgreSQL and RabbitMQ instances for end-to-end API testing.

**Requirements**: Docker must be running for integration tests to work.

### Project Structure

```
test/integration/DonkeyWork.Agents.Integration.Tests/
├── Infrastructure/
│   ├── Containers/
│   │   ├── PostgresContainerFixture.cs    # pgvector/pgvector:pg17 container
│   │   ├── RabbitMqContainerFixture.cs    # RabbitMQ with streams plugin
│   │   └── InfrastructureFixture.cs       # Combined fixture for xUnit collection
│   ├── Authentication/
│   │   ├── TestAuthenticationHandler.cs   # Bypasses JWT auth, sets IdentityContext
│   │   └── TestUser.cs                    # Test user data (default + random)
│   └── Factories/
│       └── IntegrationTestWebApplicationFactory.cs
├── Base/
│   ├── IntegrationTestBase.cs             # Database reset via Respawn
│   └── ControllerIntegrationTestBase.cs   # HTTP client helpers
├── Helpers/
│   └── TestDataBuilder.cs                 # Request/response builders
├── Fixtures/
│   └── IntegrationTestCollection.cs       # xUnit collection definition
└── Tests/Controllers/
    ├── AgentsControllerTests.cs
    ├── AgentVersionsControllerTests.cs
    └── ...
```

### Running Integration Tests

```bash
# Run all integration tests
dotnet test test/integration/DonkeyWork.Agents.Integration.Tests/

# Run specific controller tests
dotnet test --filter "AgentsControllerTests"

# Run with verbose output
dotnet test test/integration/DonkeyWork.Agents.Integration.Tests/ --logger "console;verbosity=detailed"
```

### Test Authentication

The `TestAuthenticationHandler` bypasses Keycloak and directly sets `IdentityContext`:

```csharp
// Default test user (used when no headers specified)
TestUser.Default.UserId = Guid.Parse("11111111-1111-1111-1111-111111111111")

// Switch users via HTTP headers
Client.DefaultRequestHeaders.Add("X-Test-User-Id", user.UserId.ToString());

// Or use helper method
SetTestUser(TestUser.CreateRandom());
```

### Writing Controller Tests

```csharp
[Trait("Category", "Integration")]
public class MyControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/myresource";

    public MyControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure) { }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = TestDataBuilder.CreateMyResourceRequest();

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Get_ResourceBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<MyResourceV1>(BaseUrl, TestDataBuilder.CreateMyResourceRequest());

        // Act - Try to get as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await GetResponseAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

### TestDataBuilder Requirements

When building test data for agent versions, ensure:

1. **ReactFlow data and NodeConfigurations must match** - every node ID in ReactFlow needs a corresponding entry in NodeConfigurations
2. **StartNodeConfiguration requires**: `name` (lowercase a-z, 0-9, -, _) and `inputSchema` (JsonElement)
3. **EndNodeConfiguration requires**: `name` only (outputSchema is optional)

```csharp
// Correct - matching node IDs and required properties
var reactFlowData = """
{
    "nodes": [
        { "id": "node-1", "type": "start", "position": { "x": 100, "y": 100 }, "data": {} },
        { "id": "node-2", "type": "end", "position": { "x": 400, "y": 100 }, "data": {} }
    ],
    "edges": [{ "id": "e1", "source": "node-1", "target": "node-2" }],
    "viewport": { "x": 0, "y": 0, "zoom": 1 }
}
""";
var nodeConfigurations = """
{
    "node-1": { "name": "start-node", "inputSchema": { "type": "object" } },
    "node-2": { "name": "end-node" }
}
""";
```

### Key Behaviors to Test

1. **User isolation** - Resources created by user A should return 404 for user B
2. **CRUD operations** - Create, Read, Update, Delete with proper status codes
3. **Cascade deletion** - Deleting parent entities removes children
4. **Pagination** - List endpoints with offset/limit
5. **Validation** - Invalid requests return 400 with error details

## Pre-Push Verification

**IMPORTANT**: Always run these checks before pushing code to ensure CI will pass.

### Backend (from repo root)
```bash
# Build and test
dotnet build DonkeyWork.Agents.sln
dotnet test DonkeyWork.Agents.sln
```

### Frontend (from src/frontend)
```bash
# Type check, lint, test, and build
npm run lint
npx tsc --noEmit
npm run test:run
npm run build
```

### Quick Check Script
```bash
# Run from repo root - verifies both backend and frontend
dotnet build DonkeyWork.Agents.sln && \
dotnet test DonkeyWork.Agents.sln && \
cd src/frontend && npm run lint && npx tsc --noEmit && npm run build
```
