# M2: MCP Server — Native Tools

## Overview

Implement the MCP server infrastructure and expose platform-provided native tools (todos, milestones, notes).

## Goals

1. Implement MCP server using Microsoft MCP SDK
2. Expose native tools for todos, milestones, notes
3. Implement API key and JWT authentication
4. Host at `mcp.agents.donkeywork.dev` (separate subdomain due to k3s routing: `/` → frontend, `/api` → backend)

## Deliverables

### MCP Server Infrastructure

- [ ] Add Microsoft.Extensions.AI.McpServer NuGet package
- [ ] Create MCP server module (`DonkeyWork.Agents.Mcp.*`)
- [ ] Configure MCP endpoints in ASP.NET Core
- [ ] Implement tool discovery/registration system
- [ ] Add `McpToolAttribute` with provider annotation

### Native Tools

> **Note:** Todos, Milestones, and Notes already exist in the Projects module.
> Tool classes will live in `DonkeyWork.Agents.Projects.Api` and wrap existing services.
> Todos are exposed as "tasks" in MCP tools.

- [ ] Tasks tools (wraps `ITodoService`, exposed as "tasks")
  - [ ] `tasks_list` — List user's tasks
  - [ ] `tasks_get` — Get task by ID
  - [ ] `tasks_create` — Create new task
  - [ ] `tasks_update` — Update task
  - [ ] `tasks_delete` — Delete task
- [ ] Milestones tools (wraps `IMilestoneService`)
  - [ ] `milestones_list` — List user's milestones
  - [ ] `milestones_get` — Get milestone by ID
  - [ ] `milestones_create` — Create new milestone
  - [ ] `milestones_update` — Update milestone
  - [ ] `milestones_delete` — Delete milestone
- [ ] Notes tools (wraps `INoteService`)
  - [ ] `notes_list` — List user's notes
  - [ ] `notes_get` — Get note by ID
  - [ ] `notes_create` — Create new note
  - [ ] `notes_update` — Update note
  - [ ] `notes_delete` — Delete note

### Authentication (MVP: API Key Only)

> **Note:** API key infrastructure already exists in the Credentials module.
> - `UserApiKeyEntity` — stores user API keys (encrypted)
> - `IUserApiKeyService.ValidateAsync()` — validates key, returns user ID
> - `ApiKeyAuthenticationHandler` — validates via `X-Api-Key` header, sets `IIdentityContext`

- [ ] Integrate existing `ApiKeyAuthenticationHandler` with MCP endpoints
- [ ] JWT Bearer authentication (post-MVP)
- [ ] MCP OAuth dance (post-MVP, see M4)

### Tool Metadata & Attributes

- [ ] `McpToolProviderAttribute` — Class-level provider designation
  - `Provider` — Provider enum (DonkeyWork, Microsoft, Google)
- [ ] `McpToolAttribute` — Method-level tool definition
  - `Name` — Tool name
  - `Description` — Tool description
  - `RequiredScopes` — OAuth scopes needed (for tools requiring user tokens)
- [ ] Schema generation from tool input types
- [ ] Tool discovery at startup
- [ ] Extension annotations in tool metadata (`_meta` field)

## Architecture

```
┌─────────────────────────────────────────────────────┐
│              DonkeyWork MCP Server                  │
│              mcp.agents.donkeywork.dev              │
├─────────────────────────────────────────────────────┤
│  Authentication Layer (existing)                    │
│  └── ApiKeyAuthenticationHandler                    │
├─────────────────────────────────────────────────────┤
│  Tool Registry                                      │
│  ├── Discovery (reflection-based, not DI)           │
│  └── Schema Generation                              │
├─────────────────────────────────────────────────────┤
│  Native Tools (from Projects module)                │
│  ├── tasks_*      (wraps TodoService)               │
│  ├── milestones_*                                   │
│  └── notes_*                                        │
└─────────────────────────────────────────────────────┘
```

## Module Structure

Two separate projects:

### 1. MCP Server (`src/mcp/DonkeyWork.Agents.Mcp.Server/`)
- MCP protocol handling
- Tool discovery via reflection
- Authentication integration
- References tooling assemblies

### 2. MCP Tooling (`src/mcp/DonkeyWork.Agents.Mcp.Tooling.Contracts/`)
- `McpToolProviderAttribute`
- `McpToolAttribute`
- `McpToolProvider` enum
- Base classes/interfaces for tools

### 3. Native Tools (in Projects module)

```
src/projects/DonkeyWork.Agents.Projects.Api/
└── McpTools/
    ├── TasksTools.cs        # wraps ITodoService, exposes as tasks_*
    ├── MilestonesTools.cs
    └── NotesTools.cs
```

MCP Server discovers tools via reflection across all loaded assemblies.

## Tool Attribute Design

### Provider Enum

```csharp
/// <summary>
/// Identifies the provider/vendor of an MCP tool.
/// Used for organization, filtering, and future billing/metering.
/// </summary>
public enum McpToolProvider
{
    /// <summary>
    /// Native DonkeyWork platform tools.
    /// </summary>
    DonkeyWork = 0,

    /// <summary>
    /// Microsoft Graph, Azure, etc. integrations.
    /// </summary>
    Microsoft = 1,

    /// <summary>
    /// Google Workspace, GCP, etc. integrations.
    /// </summary>
    Google = 2
}
```

### Tool Class Attribute

```csharp
/// <summary>
/// Marks a class as containing MCP tool methods.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class McpToolProviderAttribute : Attribute
{
    /// <summary>
    /// The provider/vendor for all tools in this class.
    /// </summary>
    public McpToolProvider Provider { get; set; } = McpToolProvider.DonkeyWork;
}
```

### Tool Method Attribute

```csharp
/// <summary>
/// Marks a method as an MCP tool.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class McpToolAttribute : Attribute
{
    /// <summary>
    /// Tool name (snake_case identifier).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Human-readable display name for UI.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Tool description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Icon identifier or URL for UI display.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// OAuth scopes required to use this tool.
    /// If specified, user must have granted these scopes.
    /// </summary>
    public string[]? RequiredScopes { get; set; }

    // --- MCP Standard Annotations ---

    /// <summary>
    /// If true, the tool does not modify its environment (read-only).
    /// </summary>
    public bool ReadOnlyHint { get; set; } = false;

    /// <summary>
    /// If true, the tool may perform destructive updates (delete, overwrite).
    /// Only meaningful when ReadOnlyHint is false.
    /// </summary>
    public bool DestructiveHint { get; set; } = false;

    /// <summary>
    /// If true, repeated calls with same args have no additional effect.
    /// Only meaningful when ReadOnlyHint is false.
    /// </summary>
    public bool IdempotentHint { get; set; } = false;

    /// <summary>
    /// If true, tool interacts with external entities (APIs, services).
    /// </summary>
    public bool OpenWorldHint { get; set; } = false;
}
```

### Usage Example

```csharp
[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class TasksTools
{
    private readonly ITodoService _todoService;

    public TasksTools(ITodoService todoService) => _todoService = todoService;

    [McpTool(
        Name = "tasks_list",
        Title = "List Tasks",
        Description = "List all tasks for the current user",
        Icon = "list",
        ReadOnlyHint = true,
        OpenWorldHint = false)]
    public async Task<IEnumerable<TodoV1>> ListTasks(CancellationToken ct)
    {
        return await _todoService.ListAsync(ct);
    }

    [McpTool(
        Name = "tasks_create",
        Title = "Create Task",
        Description = "Create a new task",
        Icon = "plus",
        ReadOnlyHint = false,
        DestructiveHint = false,
        IdempotentHint = false)]
    public async Task<TodoV1> CreateTask(
        [Description("Task title")] string title,
        [Description("Task description")] string? description,
        CancellationToken ct)
    {
        var request = new CreateTodoRequestV1 { Title = title, Description = description };
        return await _todoService.CreateAsync(request, ct);
    }

    [McpTool(
        Name = "tasks_delete",
        Title = "Delete Task",
        Description = "Permanently delete a task",
        Icon = "trash",
        ReadOnlyHint = false,
        DestructiveHint = true,  // This is a destructive operation
        IdempotentHint = true)]  // Deleting same ID twice has no additional effect
    public async Task DeleteTask(
        [Description("Task ID")] Guid id,
        CancellationToken ct)
    {
        await _todoService.DeleteAsync(id, ct);
    }
}

[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.Google)]
public class GoogleDriveTools
{
    [McpTool(
        Name = "google_drive_upload",
        Title = "Upload to Google Drive",
        Description = "Upload a file to Google Drive",
        Icon = "cloud-upload",
        RequiredScopes = new[] { "drive.file" },
        ReadOnlyHint = false,
        OpenWorldHint = true)]  // Interacts with external Google API
    public async Task<DriveFileV1> UploadFile(
        [Description("File name")] string fileName,
        [Description("File content as base64")] string content,
        CancellationToken ct) { }
}
```

### Tool Metadata in MCP Response

Tools expose metadata via standard MCP annotations and custom `_meta` extension:

```json
{
  "name": "tasks_delete",
  "description": "Permanently delete a task",
  "inputSchema": { ... },
  "annotations": {
    "title": "Delete Task",
    "readOnlyHint": false,
    "destructiveHint": true,
    "idempotentHint": true,
    "openWorldHint": false
  },
  "_meta": {
    "provider": "DonkeyWork",
    "icon": "trash"
  }
}
```

### Annotation Reference

| Annotation | Type | Description |
|------------|------|-------------|
| `title` | string | Human-friendly display name |
| `readOnlyHint` | bool | Tool does not modify its environment |
| `destructiveHint` | bool | Tool may delete or overwrite data |
| `idempotentHint` | bool | Repeated calls have no additional effect |
| `openWorldHint` | bool | Tool interacts with external systems |

> **Note**: Clients MUST NOT rely solely on these hints for security decisions — they are untrusted metadata.

See [MCP Tool Annotations](https://modelcontextprotocol.io/legacy/concepts/tools) for spec details.

## Dependencies

- M1: Orchestration Rename (for interfaces schema)

## Open Questions

- Tool versioning strategy?
- Rate limiting per user?
- Error response format?

## Microsoft MCP SDK Implementation

Based on research, the implementation will use the official Microsoft MCP C# SDK.

### NuGet Packages

```xml
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.7.0-preview.1" />
```

| Package | Purpose |
|---------|---------|
| `ModelContextProtocol.AspNetCore` | ASP.NET Core extensions (HTTP/SSE transport) |
| `ModelContextProtocol` | Hosting and DI extensions |
| `ModelContextProtocol.Core` | Core SDK (low-level) |

> **Note**: Packages are in preview. Check [NuGet](https://www.nuget.org/profiles/ModelContextProtocol) for latest versions.

### Basic Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()           // HTTP POST + SSE
    .WithToolsFromAssembly();       // Auto-discover [McpServerToolType]

var app = builder.Build();

app.MapMcp(pattern: "mcp");  // Creates /mcp/messages and /mcp/sse

app.Run();
```

### Tool Definition Pattern

```csharp
[McpServerToolType]
public class TasksTools
{
    private readonly ITaskService _taskService;
    private readonly IIdentityContext _identityContext;

    public TasksTools(ITaskService taskService, IIdentityContext identityContext)
    {
        _taskService = taskService;
        _identityContext = identityContext;
    }

    [McpServerTool(Name = "tasks.list")]
    [Description("List all tasks for the current user")]
    public async Task<IEnumerable<TaskV1>> ListTasks(CancellationToken ct = default)
    {
        return await _taskService.ListAsync(ct);
    }

    [McpServerTool(Name = "tasks.create")]
    [Description("Create a new task")]
    public async Task<TaskV1> CreateTask(
        [Description("Task title")] string title,
        [Description("Task description")] string? description = null,
        CancellationToken ct = default)
    {
        var request = new CreateTaskRequestV1 { Title = title, Description = description };
        return await _taskService.CreateAsync(request, ct);
    }
}
```

### Transport

- **Client → Server**: HTTP POST to `/mcp/messages`
- **Server → Client**: SSE stream from `/mcp/sse`
- Integrates with ASP.NET Core middleware pipeline

### Authentication Integration

```csharp
// API Key authentication
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null);

// Protect MCP endpoints
app.MapMcp().RequireAuthorization();
```

## References

- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [NuGet - ModelContextProtocol Packages](https://www.nuget.org/profiles/ModelContextProtocol)
- [Build MCP Server in C# - .NET Blog](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)
- [GitHub - modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)
- [Microsoft Learn - Get Started with MCP](https://learn.microsoft.com/en-us/dotnet/ai/get-started-mcp)
