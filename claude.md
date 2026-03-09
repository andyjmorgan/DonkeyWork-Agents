Project is a Modular monolith.

## Prerequisites

- **.NET 10 SDK** is required to build and test this project
- Install via script: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0`
- After installation, add to PATH: `export PATH="$HOME/.dotnet:$PATH"`

## DonkeyWork Project Management Workflow

When assigned a project or milestone via the DonkeyWork MCP tools, follow this workflow:

### Determining What to Work On

Before starting work, assess the full backlog:

1. **List all projects** - Use `mcp__donkeywork__projects_list` to see all projects
2. **List milestones and tasks** - For each active project, use `mcp__donkeywork__milestones_list` and `mcp__donkeywork__tasks_list_by_project` to see all work items
3. **Evaluate priorities** - Consider:
   - Status (items already `InProgress` take precedence)
   - Due dates
   - Dependencies (blocked items wait, blockers come first)
   - Priority field on tasks (`Critical` > `High` > `Medium` > `Low`)
   - Sort order
4. **Adjust priorities** - Use update tools to shift `sortOrder` or `priority` of items as appropriate based on the current state
5. **Select next item** - Pick the highest priority unblocked item and proceed with the workflow below

### Workflow Steps

1. **Mark as In Progress** - Immediately update the project/milestone/task status to `InProgress` using the appropriate update tool
2. **Read Requirements** - Use `mcp__donkeywork__projects_get` or `mcp__donkeywork__milestones_get` to read the full content and success criteria
3. **Research Codebase** - Explore the local codebase to understand existing patterns, related code, and dependencies
4. **Create Plan** - Document an implementation plan with clear steps
5. **Add Plan as Note** - Create a note attached to the project/milestone using `mcp__donkeywork__notes_create` with the plan content (use `projectId` or `milestoneId` parameter)
6. **Execute Plan** - Implement the plan step by step
7. **Verify** - Run build, test, and lint checks:
   ```bash
   dotnet build DonkeyWork.Agents.sln && \
   dotnet test DonkeyWork.Agents.sln && \
   cd src/frontend && npm run lint && npx tsc --noEmit && npm run build
   ```
8. **Complete** - If all checks pass, update status to `Completed` and commit/push to main

### Key Principles

- **Assess before acting** - Always read all projects, milestones, and tasks to understand the full picture before selecting work
- **IMMEDIATELY mark InProgress** - Before starting any work on a task, mark it as `InProgress` using `mcp__donkeywork__tasks_update`. This is critical for tracking what's being worked on.
- **Reprioritize as needed** - Adjust `sortOrder` and `priority` fields based on new information, dependencies, or urgency
- **Parallelize when safe** - Use the Task tool to spawn parallel agents for independent work streams (see below)
- **Update progress** - Keep status and notes updated as work progresses
- **Reference the plan** - Always refer back to the plan note during implementation
- **Document blockers** - If blocked, add notes explaining the issue and update status to `OnHold` if needed
- **Complete the loop** - After finishing an item, return to "Determining What to Work On" to select the next item
- **Save research to notes** - Any research performed (codebase exploration, technical decisions, API analysis, etc.) should be saved as notes attached to the relevant project/milestone using `mcp__donkeywork__notes_create`. This preserves knowledge across sessions and prevents re-doing the same research later.

### Parallel Execution with Agents

When multiple work items can proceed independently without conflicts, use the Task tool to spawn parallel agents:

**Safe to parallelize:**
- Backend work vs Frontend work (different codebases)
- Independent modules that don't share files
- Research/exploration tasks alongside implementation
- Tests for different modules

**Do NOT parallelize:**
- Work touching the same files
- Database migrations (run sequentially)
- Tasks with dependencies on each other
- Work in the same module/directory

**How to parallelize:**
```
Use a single message with multiple Task tool calls:
- Task 1: "Implement backend API endpoint for feature X"
- Task 2: "Implement frontend component for feature X"
```

Each agent should:
1. Mark its assigned task/milestone as `InProgress`
2. Create its own plan note
3. Execute independently
4. Run verification for its area (backend OR frontend)
5. Report back when done (do not mark complete yet)

### Cross-Cutting Concerns

For features that span multiple areas (e.g., new API endpoint + frontend UI + database changes):

1. **Plan for convergence** - Before spawning agents, identify:
   - What each agent will produce
   - Integration points between the work streams
   - What needs to be verified together

2. **Spawn parallel agents** - Each handles its independent portion:
   ```
   Agent 1 (Backend): "Add API endpoint for feature X, including migrations"
   Agent 2 (Frontend): "Add UI components for feature X, mock the API initially"
   ```

3. **Converge results** - After agents complete:
   - Review all changes together
   - Ensure API contracts match between frontend and backend
   - Update frontend to use real API (remove mocks)
   - Run full integration verification:
     ```bash
     dotnet build DonkeyWork.Agents.sln && \
     dotnet test DonkeyWork.Agents.sln && \
     cd src/frontend && npm run lint && npx tsc --noEmit && npm run test:run && npm run build
     ```

4. **Single commit/push** - Only after convergence verification passes:
   - Mark all related tasks/milestones as `Completed`
   - Commit all changes together with a unified commit message
   - Push to main

### Available MCP Tools

| Tool | Purpose |
|------|---------|
| `mcp__donkeywork__projects_list` | List all projects |
| `mcp__donkeywork__projects_get` | Get project details |
| `mcp__donkeywork__projects_update` | Update project status/content |
| `mcp__donkeywork__milestones_list` | List milestones for a project |
| `mcp__donkeywork__milestones_get` | Get milestone details |
| `mcp__donkeywork__milestones_update` | Update milestone status/content |
| `mcp__donkeywork__tasks_list_by_project` | List tasks for a project |
| `mcp__donkeywork__tasks_list_by_milestone` | List tasks for a milestone |
| `mcp__donkeywork__tasks_update` | Update task status |
| `mcp__donkeywork__notes_create` | Create a note (attach to project/milestone via IDs) |
| `mcp__donkeywork__notes_list_by_project` | List notes for a project |
| `mcp__donkeywork__notes_list_by_milestone` | List notes for a milestone |

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
- **Message Broker:** NATS JetStream
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

## EF Core Migrations

**IMPORTANT**: NEVER manually write migration files. Always use the EF Core CLI tools.

### Creating Migrations

```bash
# From repo root - always specify the startup project and project
dotnet ef migrations add MigrationName \
  --startup-project src/DonkeyWork.Agents.Api \
  --project src/common/DonkeyWork.Agents.Persistence
```

### Applying Migrations

```bash
# Apply all pending migrations
dotnet ef database update \
  --startup-project src/DonkeyWork.Agents.Api \
  --project src/common/DonkeyWork.Agents.Persistence
```

### Data-Only Migrations

For migrations that only modify data (TRUNCATE, DELETE, UPDATE), use `migrationBuilder.Sql()`:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("TRUNCATE TABLE schema.table_name CASCADE;");
}
```

The migration file will be auto-generated - only add the SQL statements to the Up/Down methods.

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

## JSON Polymorphism

When using `[JsonPolymorphic]` for type hierarchies:

1. **Use `nameof()` for discriminators** - never snake_case strings:
```csharp
// CORRECT
[JsonPolymorphic]
[JsonDerivedType(typeof(TextContentPart), nameof(TextContentPart))]
[JsonDerivedType(typeof(ImageContentPart), nameof(ImageContentPart))]
public abstract class ContentPart { }

// WRONG - do not use snake_case or custom strings
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextContentPart), "text")]
```

2. **Do NOT define explicit Type properties** - the discriminator handles serialization automatically:
```csharp
// WRONG - causes conflict with discriminator
public abstract string Type { get; }

// CORRECT - no Type property needed, [JsonPolymorphic] handles it
```

3. **Use default `$type` discriminator** - don't customize `TypeDiscriminatorPropertyName` unless required for external API compatibility.

4. **Enable `AllowOutOfOrderMetadataProperties`** - set this on `JsonSerializerOptions` to handle JSON where `$type` isn't the first property:
```csharp
var options = new JsonSerializerOptions
{
    AllowOutOfOrderMetadataProperties = true
};
```
This is already configured globally in `Program.cs` and in services that use custom `JsonSerializerOptions`.

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

Integration tests use TestContainers to spin up real PostgreSQL and NATS instances for end-to-end API testing.

**Requirements**: Docker must be running for integration tests to work.

### Project Structure

```
test/integration/DonkeyWork.Agents.Integration.Tests/
├── Infrastructure/
│   ├── Containers/
│   │   ├── PostgresContainerFixture.cs    # pgvector/pgvector:pg17 container
│   │   ├── NatsContainerFixture.cs         # NATS with JetStream
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

## Sandbox Module (Code Execution)

The `src/sandbox/` directory contains independently deployable code execution services. These are **separate containers** from the modular monolith API - they share the solution but have no code dependencies on the monolith.

### Structure

```
src/sandbox/
├── CodeSandbox.Contracts/     # Shared models, DTOs, service interfaces
├── CodeSandbox.Manager/       # Orchestrates sandbox lifecycle (port 8668)
├── CodeSandbox.Executor/      # Runs user code inside Kata VM pods (port 8666)
├── CodeSandbox.AuthProxy/     # OAuth proxy for sandbox auth (ports 8080/8081)
└── Directory.Packages.props   # Central NuGet package versions for sandbox

test/sandbox/
├── CodeSandbox.Executor.IntegrationTests/  # Testcontainers-based integration tests
└── Directory.Packages.props                # Test package versions
```

### Components

- **Manager** (`CodeSandbox.Manager`): Kubernetes-aware service that creates/destroys sandbox pods, proxies code execution requests, and provides WebSocket terminal access. Uses SSE for streaming output.
- **Executor** (`CodeSandbox.Executor`): Runs inside each sandbox pod (Kata container for VM isolation). Executes code, manages files, and streams results back to Manager.
- **AuthProxy** (`CodeSandbox.AuthProxy`): Handles OAuth authentication for sandbox endpoints.
- **Contracts** (`CodeSandbox.Contracts`): Shared request/response models used by Manager and Executor.

### Docker Images

| Image | Dockerfile | Base |
|-------|-----------|------|
| `sandbox-manager` | `src/sandbox/CodeSandbox.Manager/Dockerfile` | `mcr.microsoft.com/dotnet/aspnet:10.0` |
| `sandbox-executor` | `src/sandbox/CodeSandbox.Executor/Dockerfile` | Custom base with SDK + languages |
| `sandbox-authproxy` | `src/sandbox/CodeSandbox.AuthProxy/Dockerfile` | `mcr.microsoft.com/dotnet/aspnet:10.0` |
| `codesandbox-executor-base` | `src/sandbox/CodeSandbox.Executor/Dockerfile.base` | Ubuntu + .NET SDK + Node.js + Python |

### Building & Running

```bash
# Build all sandbox projects
dotnet build src/sandbox/CodeSandbox.Manager/
dotnet build src/sandbox/CodeSandbox.Executor/
dotnet build src/sandbox/CodeSandbox.AuthProxy/

# Run via docker-compose (includes sandbox-manager service)
docker compose up sandbox-manager

# Run integration tests (requires Docker)
dotnet test test/sandbox/CodeSandbox.Executor.IntegrationTests/
```

### CI/CD

Sandbox components use **path-based selective builds** in GitHub Actions:
- Changes in `src/sandbox/**` or `test/sandbox/**` trigger sandbox-specific jobs
- Changes outside sandbox don't rebuild sandbox images (and vice versa)
- `Dockerfile.runtime` variants are used in CI for faster builds (pre-built artifacts, no SDK layer)

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
