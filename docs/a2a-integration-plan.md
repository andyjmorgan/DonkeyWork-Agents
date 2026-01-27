# A2A SDK Integration Plan

## Overview

Integrate the [A2A (Agent-to-Agent) C# SDK](https://github.com/a2aproject/a2a-sdk-dotnet) into DonkeyWork-Agents to expose published agents via the A2A protocol. This enables external clients and other agents to discover and invoke DonkeyWork agents using the standardised A2A JSON-RPC interface.

---

## A2A SDK Summary

The `A2A` and `A2A.AspNetCore` NuGet packages (protocol v0.2.6) provide:

| Component | Purpose |
|---|---|
| `AgentCard` | Machine-readable JSON manifest describing an agent's capabilities, input/output modes, and skills |
| `ITaskHandler` | Interface for handling incoming A2A messages and managing task lifecycle (`Submitted → Working → Completed/Failed`) |
| `MapA2A()` | Extension method on `IEndpointRouteBuilder` that wires up card serving + JSON-RPC message handling |
| `A2AClient` / `A2ACardResolver` | Client-side classes for discovering remote agent cards and sending messages |

---

## Mapping DonkeyWork Concepts to A2A

| DonkeyWork | A2A |
|---|---|
| `Agent` (name, description) | `AgentCard` (name, description, url, skills) |
| `AgentVersion.InputSchema` | Skill input schema |
| `POST /agents/{id}/execute` | `message/send` JSON-RPC |
| SSE streaming execution | `message/stream` SSE |
| Execution result / output | A2A `Artifact` |

---

## Architecture

### New Module

Following the existing modular pattern, add:

```
src/
└── a2a/
    └── DonkeyWork.Agents.A2A.Api/
        ├── DonkeyWork.Agents.A2A.Api.csproj
        ├── DependencyInjection.cs
        ├── Controllers/
        │   └── A2ACardsController.cs      # Internal API: list/manage cards
        ├── Services/
        │   ├── IA2ACardService.cs          # Build AgentCards from published agents
        │   ├── A2ACardService.cs
        │   └── DonkeyWorkTaskHandler.cs    # ITaskHandler bridging to IAgentOrchestrator
        └── Models/
            └── A2ACardResponseV1.cs
```

### Two Endpoint Surfaces

**1. Internal API (controller-based, behind `[Authorize]`)**

- `GET /api/v1/a2a/cards` — list all of the current user's published agents as A2A cards.
- `GET /api/v1/a2a/cards/{agentId}` — get a single agent's A2A card.

This allows the frontend/management UI to see what's exposed.

**2. A2A Protocol Endpoints (minimal API via `MapA2A`)**

- `/.well-known/agent.json` or `/a2a/{agentId}` — serves the `AgentCard` and handles `message/send` / `message/stream` JSON-RPC calls.
- These sit outside the `[Authorize]` boundary (A2A defines its own auth model).

---

## Key Implementation Pieces

### A2ACardService

Queries `IAgentService` and `IAgentVersionService` to build `AgentCard` objects from published agents:

- Maps agent `Name` / `Description` to card metadata.
- Converts `AgentVersion.InputSchema` (JSON Schema) into A2A skill definitions.
- Sets the card URL to the agent's A2A protocol endpoint.

### DonkeyWorkTaskHandler

Implements `ITaskHandler` to bridge A2A messages into the existing execution pipeline:

1. Receives an A2A `Message` (text content from the caller).
2. Extracts/maps input to the agent's `InputSchema` format.
3. Calls `IAgentOrchestrator.ExecuteAsync` with the mapped input.
4. Reads execution results and maps them back to A2A `Artifact` responses.
5. For streaming: reads from `IExecutionStreamService` and emits A2A-compatible SSE events.

### DependencyInjection

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddA2AApi(this IServiceCollection services)
    {
        services.AddScoped<IA2ACardService, A2ACardService>();
        services.AddScoped<DonkeyWorkTaskHandler>();
        return services;
    }

    public static IEndpointRouteBuilder MapA2AEndpoints(this IEndpointRouteBuilder app)
    {
        // Register A2A protocol endpoints for agent discovery and invocation
        // Uses MapA2A() from A2A.AspNetCore
        return app;
    }
}
```

### Changes to Program.cs

```csharp
// Service registration
builder.Services.AddA2AApi();

// After app.MapControllers():
app.MapA2AEndpoints();
```

### Changes to Main API .csproj

```xml
<ProjectReference Include="..\a2a\DonkeyWork.Agents.A2A.Api\DonkeyWork.Agents.A2A.Api.csproj" />
```

---

## Design Decisions

### Authentication

The A2A protocol endpoints operate outside the existing Keycloak `[Authorize]` boundary. Options:

- **Phase 1**: `AllowAnonymous` — suitable for internal/dev use.
- **Phase 2**: API key header auth (A2A convention) or mTLS.

The internal card management endpoints (`/api/v1/a2a/cards`) remain behind standard `[Authorize]`.

### Which Agents Are Exposed

Not all agents should be exposed via A2A. Options:

- **Option A** (simpler): Expose all agents that have a `CurrentVersionId` (i.e. at least one published version).
- **Option B** (more control): Add an `IsA2AEnabled` flag to the `Agent` entity, requiring explicit opt-in.

Recommend starting with **Option A** and adding the flag later if needed.

### Streaming

Reuse the existing RabbitMQ stream infrastructure. `DonkeyWorkTaskHandler` reads from `IExecutionStreamService` and translates `TokenDelta` / `ExecutionCompleted` / `ExecutionFailed` events into A2A streaming responses.

### .NET 10 Compatibility

The A2A SDK targets .NET Standard 2.0 / .NET 8+. This should be compatible with .NET 10 but needs verification during implementation.

---

## Implementation Steps

1. Create the `DonkeyWork.Agents.A2A.Api` project and add NuGet references (`A2A`, `A2A.AspNetCore`).
2. Implement `IA2ACardService` / `A2ACardService` to build cards from published agents.
3. Implement `A2ACardsController` for the internal card listing API.
4. Implement `DonkeyWorkTaskHandler` bridging A2A messages to `IAgentOrchestrator`.
5. Wire up `MapA2A` protocol endpoints.
6. Register the module in `Program.cs` and update the main `.csproj`.
7. Verify build and test with a sample agent invocation.
