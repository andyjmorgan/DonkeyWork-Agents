# Orchestration Vision

> Evolution of the agent platform to support chat, deterministic workflows, and tool-augmented orchestrations.

## Overview

This document outlines the vision for evolving the current agent system into a comprehensive orchestration platform. The core primitive—**Orchestration**—serves as the backbone for multiple interaction paradigms.

### Interfaces

| Interface | Description | MVP |
|-----------|-------------|-----|
| **MCP** | Expose orchestrations as tools via our MCP server | ✓ |
| **A2A** | Agent-to-Agent protocol with agent cards | ✓ |
| **Chat** | Conversational interface with multi-turn memory | ✓ |
| **Webhook** | Inbound HTTP triggers for deterministic flows | ✗ |

### Terminology Change ✓

| Old | New | Status |
|-----|-----|--------|
| Agent | Orchestration | ✓ Done |
| Agent Version | Orchestration Version | ✓ Done |

---

## Core Concepts

### Orchestration

The fundamental unit—a versioned, DAG-based workflow that can be exposed through multiple interfaces simultaneously.

```
Orchestration
├── Versions (draft/published)
├── InputSchema (JSON Schema)
├── OutputSchema (JSON Schema, optional)
├── ReactFlowData (frontend graph representation)
├── NodeConfigurations (backend execution config)
└── Interfaces[]
    ├── MCP     {name, description, enabled}
    ├── A2A     {name, description, enabled}
    ├── Chat    {name, description, enabled}
    └── Webhook {name, description, enabled}  [post-MVP]
```

### Interface Configuration ✓

Each orchestration can enable multiple interfaces. Each interface has its own:

| Field | Description |
|-------|-------------|
| `name` | How it appears in that context (MCP tool name may differ from A2A agent name) |
| `description` | Interface-specific description |
| `enabled` | Toggle to expose/hide via this interface |

This allows one orchestration to have different names and descriptions depending on how it's accessed.

### Execution Context ✓

The execution context tracks which interface triggered the run:

```csharp
public enum ExecutionInterface
{
    Direct,    // API call to /execute endpoint
    MCP,       // Via MCP server
    A2A,       // Via A2A protocol
    Chat,      // Via chat interface
    Webhook    // Via webhook (post-MVP)
}
```

This flag is available throughout execution, allowing orchestrations to behave differently based on invocation context if needed.

---

## Interface: MCP Server

### Vision

We will build an MCP server that exposes:

1. **Native tools** — Platform-provided tools (tasks, milestones, notes)
2. **User orchestrations** — User's MCP-enabled orchestrations as tools

Users can build bespoke orchestration workflows and expose them as MCP tools to any MCP-compatible client (Claude Desktop, IDEs, custom integrations).

### Architecture

```
┌─────────────────────────────────────────────────────┐
│                   MCP Client                         │
│         (Claude Desktop, IDEs, custom apps)         │
└─────────────────────┬───────────────────────────────┘
                      │ MCP Protocol
                      ▼
┌─────────────────────────────────────────────────────┐
│              DonkeyWork MCP Server                  │
├─────────────────────────────────────────────────────┤
│  Native Tools            │  User Orchestrations     │
│  ├── tasks/*             │  ├── {orch-a}            │
│  ├── todos/*             │  ├── {orch-b}            │
│  └── milestones/*        │  └── ...                 │
└─────────────────────────────────────────────────────┘
```

### Authentication

| Method | MVP | Description | Status |
|--------|-----|-------------|--------|
| API Key | ✓ | Simple programmatic access | ✓ Done |
| JWT | ✓ | Token-based authentication | ✓ Done |
| MCP OAuth | ✓ | MCP spec OAuth dance compliance | Not started |

### Tool Schema Generation

| Source | Target |
|--------|--------|
| Orchestration `InputSchema` | MCP tool input schema |
| Interface `description` | MCP tool description |
| Interface `name` | MCP tool name |

### MCP Feature Scope

| Feature | MVP | Later | Status |
|---------|-----|-------|--------|
| Tools | ✓ | | ✓ Done (native tools) |
| Resources | | ✓ | Not started |
| Prompts | | ✓ | Not started |

---

## Interface: A2A Protocol

### Vision

Orchestrations with A2A interface enabled will be discoverable and callable via the Agent2Agent Protocol. We will:

- Generate agent cards from orchestration metadata
- Handle A2A messaging format
- Track task lifecycle per the A2A spec

### Protocol Reference

| | |
|---|---|
| **Spec Version** | 0.3 (July 2025) |
| **Governance** | Linux Foundation |
| **GitHub** | https://github.com/a2aproject/A2A |
| **Specification** | https://a2a-protocol.org/latest/specification/ |

### Key Protocol Concepts

#### Agent Card

JSON metadata document describing an agent's identity and capabilities:

```json
{
  "name": "orchestration-name",
  "description": "What this agent does",
  "provider": {
    "organization": "DonkeyWork"
  },
  "capabilities": {
    "streaming": true,
    "pushNotifications": false
  },
  "skills": [
    {
      "name": "process",
      "description": "Process input and return result",
      "inputSchema": { },
      "outputSchema": { }
    }
  ],
  "securitySchemes": { }
}
```

#### Task Lifecycle States

| State | Description |
|-------|-------------|
| `working` | Active processing |
| `completed` | Successfully finished |
| `failed` | Encountered an error |
| `canceled` | Client or agent initiated cancellation |
| `rejected` | Agent declined to process |
| `input_required` | Awaiting client response |
| `auth_required` | Needs authentication credentials |

#### Message Format

Messages contain **parts** (content segments):
- Text content
- File references (MIME type, URL)
- Structured data (JSON)
- Artifacts (composed of multiple parts)

#### Transport

- JSON-RPC 2.0 over HTTP(S)
- gRPC support (v0.3+)
- Streaming via SSE
- Push notifications (optional)

#### Core JSON-RPC Methods

| Method | Description |
|--------|-------------|
| `SendMessage` | Send message to agent |
| `SendStreamingMessage` | Stream message to agent |
| `GetTask` | Get task status |
| `ListTasks` | List tasks |
| `CancelTask` | Cancel a task |
| `SubscribeToTask` | Subscribe to task updates |

### A2A ↔ Orchestration Mapping

| A2A Concept | Orchestration Concept |
|-------------|----------------------|
| Agent | Orchestration (with A2A interface enabled) |
| Agent Card | Generated from orchestration metadata + A2A interface config |
| Task | Orchestration Execution |
| Message Parts | Execution Input/Output |
| Task State | Execution Status |

### A2A Scope

| Feature | MVP | Later | Status |
|---------|-----|-------|--------|
| Agent Card generation | ✓ | | Not started |
| Be called via A2A (server role) | ✓ | | Not started |
| Task lifecycle management | ✓ | | Not started |
| Streaming responses | ✓ | | Not started |
| Call external A2A agents (client role) | | ✓ | Not started |
| Push notifications | | ✓ | Not started |
| gRPC transport | | ✓ | Not started |

---

## Interface: Chat

### Vision

Orchestrations with Chat interface enabled provide conversational experiences with:

- Multi-turn conversation memory
- Multimodal message support (text, images, audio, files)
- Tool usage tracking with summaries for UI display
- Provider-agnostic storage with translation at call time

### Execution Model

**Per-message execution** — Each user message triggers a complete orchestration run. The orchestration is stateless; state lives in the conversation history.

```
User sends message
       │
       ▼
┌──────────────────────────────┐
│  Load conversation history   │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│  Execute orchestration       │
│  (Start → ... → End)         │
│  History injected as context │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│  Save response to            │
│  conversation:               │
│  - Assistant message         │
│  - Tool call summaries       │
│  - Token usage metadata      │
└──────────────────────────────┘
```

### Conversation Ownership

- A conversation is tied to **one chat-enabled orchestration**
- A user can have **many conversations** with that orchestration
- Conversations are **strictly user-scoped** (no cross-user access)

### Data Model

```
Conversation
├── Id
├── OrchestrationId (must have Chat interface enabled)
├── UserId
├── Title
├── CreatedAt
├── UpdatedAt
└── Messages[]

ConversationMessage
├── Id
├── ConversationId
├── Role (user | assistant | system)
├── Content (ContentPart[])
├── ToolCallSummaries[] (for UI display)
├── TokenUsage {input, output, total}
├── ModelInfo {provider, model}
├── CreatedAt
└── Metadata
```

### Message Format (Canonical)

Provider-agnostic format. Translated to/from Anthropic/OpenAI/Gemini formats at call time, similar to existing model middleware pattern.

```csharp
public abstract class ContentPart
{
    public abstract ContentPartType Type { get; }
}

public enum ContentPartType
{
    Text,
    Image,
    Audio,
    File,
    ToolUse,
    ToolResult
    // Polymorphic design allows future types
}
```

#### Content Part Types

```csharp
public class TextContentPart : ContentPart
{
    public override ContentPartType Type => ContentPartType.Text;
    public required string Text { get; set; }
}

public class ImageContentPart : ContentPart
{
    public override ContentPartType Type => ContentPartType.Image;
    public string? Base64Data { get; set; }
    public string? Url { get; set; }
    public required string MediaType { get; set; }  // image/png, image/jpeg, etc.
}

public class AudioContentPart : ContentPart
{
    public override ContentPartType Type => ContentPartType.Audio;
    public string? Base64Data { get; set; }
    public string? Url { get; set; }
    public required string MediaType { get; set; }  // audio/wav, audio/mp3, etc.
}

public class FileContentPart : ContentPart
{
    public override ContentPartType Type => ContentPartType.File;
    public required string FileName { get; set; }
    public string? Base64Data { get; set; }
    public string? Url { get; set; }
    public required string MediaType { get; set; }
}

public class ToolUseContentPart : ContentPart
{
    public override ContentPartType Type => ContentPartType.ToolUse;
    public required string ToolId { get; set; }
    public required string ToolName { get; set; }
    public required JsonElement Input { get; set; }
}

public class ToolResultContentPart : ContentPart
{
    public override ContentPartType Type => ContentPartType.ToolResult;
    public required string ToolId { get; set; }
    public required JsonElement Result { get; set; }
    public bool IsError { get; set; }
}
```

**Polymorphic design** ensures extensibility for future content types without breaking changes.

### Tool Usage in Chat

#### MVP: Server-Side Tools Only

Chat can use **platform-provided tools only**:
- Tasks
- Todos
- Milestones

User orchestrations as chat tools are **post-MVP** but the polymorphic message format keeps this door open.

#### Tool Call Summaries

Tool calls are stored as **summaries** in the conversation for UI display—not full payloads:

```csharp
public class ToolCallSummary
{
    public required string ToolName { get; set; }
    public required string Description { get; set; }  // Human-readable summary
    public bool Success { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```

This ensures:
- Conversation history UI shows what tools were invoked
- Context window is not bloated with verbose tool payloads
- Full tool_use/tool_result not stored—only summaries

### Chat Scope

| Feature | MVP | Later | Status |
|---------|-----|-------|--------|
| Multi-turn conversations | ✓ | | Not started |
| Text messages | ✓ | | Not started |
| Image messages | ✓ | | Not started |
| Audio messages | | ✓ | Not started |
| File/document messages | | ✓ | Not started |
| Server-side tools (tasks, milestones, notes) | ✓ | | ✓ Done (tools exist) |
| User orchestrations as tools | | ✓ | Not started |
| Tool call summaries in history | ✓ | | Not started |
| Streaming responses | ✓ | | Not started |

---

## Interface: Webhook (Post-MVP)

### Vision

Orchestrations with Webhook interface enabled can be triggered by external HTTP POST requests.

### Characteristics

| Aspect | Detail |
|--------|--------|
| **Direction** | Inbound only — external systems POST to trigger |
| **Authentication** | None — public endpoints |
| **Input Schema** | None — accepts any JSON payload |
| **Use Case** | Deterministic flows handling predictable webhook payloads |

### Example Use Cases

- GitHub webhook → process PR/issue events
- Stripe webhook → handle payment events
- Custom integrations → any HTTP POST trigger

### Deferred to Post-MVP

---

## Node Palette

### Current Nodes ✓

| Node | Description | Status |
|------|-------------|--------|
| Start | Entry point, input validation against InputSchema | ✓ Done |
| End | Exit point, returns output | ✓ Done |
| Model | LLM call with prompts | ✓ Done |
| MultimodalChatModel | Multimodal LLM call | ✓ Done |
| HttpRequest | HTTP requests (GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS) | ✓ Done |
| Sleep | Pause execution | ✓ Done |
| MessageFormatter | Scriban template-based formatting | ✓ Done |

### Future Nodes (Post-MVP)

| Node | Description |
|------|-------------|
| **Tool** | Invoke a platform MCP tool |
| **MCPServer** | Connect to external MCP server, invoke its tools |
| **A2AAgent** | Call an external A2A agent (client role) |
| **Orchestration** | Invoke another internal orchestration (composition) |
| **Conditional** | Branch execution based on condition |
| **Loop** | Iterate over a collection |
| **Parallel** | Fan-out parallel execution |

---

## Authentication & Authorization

### MCP Server Authentication

| Method | MVP | Description |
|--------|-----|-------------|
| API Key | ✓ | Simple key-based auth |
| JWT | ✓ | Token-based auth |
| MCP OAuth | ✓ | MCP spec OAuth dance |

### A2A Server Authentication

Defined in Agent Card's `securitySchemes`:
- API Key
- Bearer token
- OAuth 2.0
- OpenID Connect

### Multi-tenancy

**Strictly user-scoped for MVP.**

- User A cannot access User B's orchestrations
- No cross-user sharing via MCP, A2A, or Chat
- All entities filtered by `UserId` via existing DbContext global query filter

### Credential Context

**Deferred to future PR.**

Will address:
- Credential propagation when orchestration invokes downstream services
- Caller-provided vs owner-provided credentials
- Credential scoping per interface

---

## UI Approach

### Decision

**Own the UI stack** — not adopting AG-UI or A2UI for MVP.

### Rationale

- AG-UI (CopilotKit) and A2UI (Google) are still young (v0.x)
- Current architecture (RabbitMQ streams → SSE → custom frontend) provides full control
- Avoids external dependencies during MVP
- Can evaluate standards adoption post-MVP when landscape stabilizes

### Future Consideration

| Protocol | What It Does | Reconsider When |
|----------|--------------|-----------------|
| [AG-UI](https://docs.ag-ui.com/) | Event-based streaming protocol (agent ↔ frontend) | If we need cross-framework compatibility |
| [A2UI](https://a2ui.org/) | Agents generate rich UI components (cards, forms) | If we want interactive widgets in chat responses |

---

## Internal Composition (Post-MVP)

### Vision

An orchestration can invoke another orchestration as a node—like a subroutine. This is separate from A2A (which is external agent-to-agent communication).

### Future Node Types for Composition

| Node | Description |
|------|-------------|
| Tool | Platform MCP tool as a node |
| MCPServer | External MCP server as a node |
| A2AAgent | External A2A agent as a node |
| Orchestration | Internal orchestration as a node |

### Deferred to Post-MVP

---

## Relationships Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                             User                                 │
└─────────────────────────────┬───────────────────────────────────┘
                              │ owns
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                        Orchestration                             │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Versions (draft/published)                               │    │
│  │ InputSchema, OutputSchema                                │    │
│  │ ReactFlowData, NodeConfigurations                        │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Interfaces (each has: name, description, enabled)        │    │
│  │ ├── MCP                                                  │    │
│  │ ├── A2A                                                  │    │
│  │ ├── Chat                                                 │    │
│  │ └── Webhook  [post-MVP]                                  │    │
│  └─────────────────────────────────────────────────────────┘    │
└───────────┬────────────────┬────────────────┬───────────────────┘
            │                │                │
    ┌───────▼───────┐ ┌──────▼──────┐ ┌──────▼──────┐
    │  MCP Server   │ │ A2A Server  │ │  Chat API   │
    │               │ │             │ │             │
    │ Native tools: │ │ Agent Card  │ │Conversation │
    │ - tasks       │ │ Task mgmt   │ │ Messages    │
    │ - milestones  │ │ A2A methods │ │ History     │
    │ - notes       │ │             │ │ Summaries   │
    │               │ │             │ │             │
    │ User orch.    │ │             │ │             │
    │ as tools      │ │             │ │             │
    └───────────────┘ └─────────────┘ └─────────────┘
```

---

## Milestones

See [milestones/](./milestones/) for detailed implementation plans.

### MVP

| # | Milestone | Dependencies | Status |
|---|-----------|--------------|--------|
| M1 | [Orchestration Rename & Interfaces](./milestones/m1-orchestration-rename.md) | — | ✓ Done |
| M2 | [MCP Server — Native Tools](./milestones/m2-mcp-server-native-tools.md) | M1 | ✓ Done |
| M3 | [Chat Interface](./milestones/m3-chat-interface.md) | M1, M2 | Not started |
| M5 | [A2A Server](./milestones/m5-a2a-server.md) | M1 | Not started |
| M7 | [MCP Server — User Orchestrations](./milestones/m7-mcp-server-user-orchestrations.md) | M1, M2 | Not started |

### Post-MVP

| # | Milestone | Dependencies | Status |
|---|-----------|--------------|--------|
| M4 | [MCP OAuth](./milestones/m4-mcp-oauth.md) | M2, M7 | Not started |

---

## Cross-Cutting Concerns

### Authentication Strategy

| Interface | MVP Auth | Post-MVP | Status |
|-----------|----------|----------|--------|
| MCP Server | API Key | JWT, MCP OAuth | ✓ API Key done |
| A2A Server | API Key | Bearer, OAuth 2.0, OIDC | Not started |
| Chat API | JWT (existing) | — | ✓ JWT exists |
| Direct Execute | JWT (existing) | — | ✓ Done |

### User Isolation ✓

All interfaces enforce strict user isolation:
- ✓ Orchestrations filtered by `UserId` via DbContext global query filter
- ✓ No cross-user access to orchestrations, conversations, or executions
- ✓ API keys scoped to individual users

### Execution Tracking ✓

All interfaces route through the same execution engine:
- ✓ `ExecutionInterface` enum tracks invocation source
- ✓ Unified execution logging and metrics
- ✓ Same streaming infrastructure (RabbitMQ → SSE)

### Hosting & Endpoints

| Interface | External URL | Internal Route |
|-----------|--------------|----------------|
| MCP Server | `mcp.agents.donkeywork.dev` | `/mcp/*` |
| A2A Server | `a2a.agents.donkeywork.dev` | `/a2a/*` |
| Chat API | `api.agents.donkeywork.dev` | `/api/v1/conversations/*` |
| Orchestrations API | `api.agents.donkeywork.dev` | `/api/v1/orchestrations/*` |

All hosted in the monolith, exposed via k3s ingress. Separate subdomains used because main domain routes `/` → frontend, `/api` → backend.

### MCP Tool Metadata ✓

Native MCP tools use custom attributes for provider tracking and OAuth scope requirements:

```csharp
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class TasksTools
{
    [McpTool(Name = "tasks_create", RequiredScopes = new[] { "tasks:write" })]
    public async Task<TaskV1> CreateTask(...) { }
}
```

| Attribute Property | Purpose |
|--------------------|---------|
| `Provider` | Vendor enum: DonkeyWork, Microsoft, Google |
| `RequiredScopes` | OAuth scopes user must grant (post-MVP) |

Tools expose metadata via `_meta` field in MCP responses for client introspection.

### Error Handling

Consistent error responses across interfaces:
- MCP: JSON-RPC error format
- A2A: JSON-RPC error format + task failed state
- Chat/REST: Problem Details (RFC 7807)

### Rate Limiting (Post-MVP)

Per-user rate limits across all interfaces:
- Track by user ID
- Shared quota or per-interface quotas (TBD)
- 429 responses with retry-after

### Observability

- Structured logging with correlation IDs
- Execution traces linked across interfaces
- Metrics: executions by interface, latency, token usage

---

## Open Questions

1. **Context window management** — For long chat conversations, how do we handle context limits? Options: summarization, sliding window, semantic retrieval.

2. **Conversation branching** — Can users "branch" a conversation to explore different response paths?

3. **Shared orchestrations** — Will we ever support sharing orchestrations between users or making them public?

4. **Rate limiting** — How do we rate limit MCP/A2A calls per user?

5. **Billing/metering** — How do we track and bill for orchestration executions across interfaces?

6. **Conversation export** — Should users be able to export conversation history?

---

## References

- [A2A Protocol Specification](https://a2a-protocol.org/latest/specification/)
- [A2A GitHub Repository](https://github.com/a2aproject/A2A)
- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [Linux Foundation A2A Announcement](https://www.linuxfoundation.org/press/linux-foundation-launches-the-agent2agent-protocol-project-to-enable-secure-intelligent-communication-between-ai-agents)
- [AG-UI Documentation](https://docs.ag-ui.com/)
- [A2UI Documentation](https://a2ui.org/)
