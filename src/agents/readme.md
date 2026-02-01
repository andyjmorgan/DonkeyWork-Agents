# Agents Module

Core agentic framework containing the orchestrator, agent execution, and node system.

## Overview

- **Frontend**: ReactFlow for visual workflow editing
- **Triggers**: API call or playground only (MVP). Webhook and scheduled triggers planned for later.
- **Streaming**: RabbitMQ Streams for event delivery, SSE for client consumption
- **Templating**: Scriban for dynamic message construction

---

## Architecture

### Unified Node Configuration

All node types follow a unified pattern with strongly-typed configuration classes in `Agents.Contracts/Nodes`:

```
Agents.Contracts/Nodes/
├── Enums/
│   ├── NodeType.cs           # Start, End, Model, HttpRequest, Sleep, MessageFormatter
│   └── ControlType.cs        # Text, TextArea, Slider, Select, Toggle, etc.
├── Configurations/
│   ├── NodeConfiguration.cs          # Abstract base class
│   ├── StartNodeConfiguration.cs     # InputSchema
│   ├── EndNodeConfiguration.cs       # OutputSchema
│   ├── ModelNodeConfiguration.cs     # Provider, SystemPrompts[], UserMessages[]
│   ├── MessageFormatterNodeConfiguration.cs
│   ├── HttpRequestNodeConfiguration.cs
│   └── SleepNodeConfiguration.cs
├── Attributes/
│   ├── ConfigurableFieldAttribute.cs # Field metadata for schema generation
│   ├── TabAttribute.cs               # Group fields into tabs
│   ├── SliderAttribute.cs            # Slider constraints
│   └── SupportVariablesAttribute.cs  # Mark fields supporting Scriban
└── Schema/
    ├── NodeConfigSchema.cs           # Generated schema for frontend
    └── INodeSchemaGenerator.cs       # Generates schema from attributes
```

### Provider Pattern for Node Execution

Node execution uses a provider pattern where methods are decorated with the node type they handle:

```csharp
[NodeProvider]
public class HttpNodeProvider
{
    [NodeMethod(NodeType.HttpRequest)]
    public async Task<HttpRequestOutput> ExecuteHttpRequestAsync(
        HttpRequestNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}

[NodeProvider]
public class TimingNodeProvider
{
    [NodeMethod(NodeType.Sleep)]
    public async Task<SleepOutput> ExecuteSleepAsync(
        SleepNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

The `GenericNodeExecutor` routes execution to the correct provider method at runtime.

---

## MVP Scope

### In Scope
- Start, End, Model (text only), HttpRequest, Sleep, MessageFormatter nodes
- Sequential execution (single path)
- RabbitMQ Streams for event streaming
- SSE and non-streaming API responses
- JSON Schema validation for input/output
- Scriban templating for dynamic fields
- Full execution tracing

### Out of Scope (Future)
- Tool/function calling in Model node
- Nested agent execution (agent calls agent)
- Human-in-the-loop (approval workflows)
- Parallel fan-out execution
- Webhook and scheduled triggers
- Crash recovery

---

## Data Model

### Agent

```
Agent
├── Id (UUID)
├── UserId (UUID) - owner
├── Name (string)
├── Description (string)
├── CurrentVersionId (UUID, nullable) - points to latest published version
└── Versions[] - navigation property
```

### Agent Version

Two separate data properties - DO NOT mix ReactFlow data with configuration data.

```
AgentVersion
├── Id (UUID)
├── AgentId (UUID) - FK to Agent
├── VersionNumber (int) - incrementing version number
├── IsDraft (bool) - true for unpublished draft
├── InputSchema (JSONB, required) - JSON Schema for input validation AND defines available template variables
├── OutputSchema (JSONB, nullable) - JSON Schema for output (optional, not validated in MVP)
├── ReactFlowData (JSONB) - complete ReactFlow export (nodes, edges, viewport, etc.)
├── NodeConfigurations (JSONB) - Dictionary<Guid, NodeConfiguration> keyed by node ID
├── CreatedAt (DateTimeOffset)
└── PublishedAt (DateTimeOffset, nullable)
```

**ReactFlowData**: Stored as-is from frontend. Contains:
- `nodes[]`: array of node objects
  - `id`: GUID string
  - `type`: node type string ("start", "model", "end", "httpRequest", "sleep", "messageFormatter")
  - `position`: { x, y }
  - `data`: frontend-specific data (labels, etc.)
- `edges[]`: array of edge objects
  - `id`: GUID string
  - `source`: source node ID
  - `target`: target node ID
- `viewport`: zoom/pan state (frontend only)

**NodeConfigurations**: Backend's source of truth for execution. Dictionary keyed by node GUID, containing typed configuration per node type. Each configuration includes a `name` field for template references.

---

## Node Types

Nodes are registered via DI using a provider pattern, allowing new node types to be added easily.

### NodeType Enum

```csharp
public enum NodeType
{
    // Flow control
    Start,
    End,

    // AI
    Model,

    // Utility
    MessageFormatter,

    // HTTP
    HttpRequest,

    // Timing
    Sleep,
}
```

### Adding a New Node Type

1. **Add NodeType enum value** in `Agents.Contracts/Nodes/Enums/NodeType.cs`
2. **Create Configuration class** in `Agents.Contracts/Nodes/Configurations/`
3. **Add provider method** in an existing or new provider class
4. **Register provider in DI** (one-time per provider)

Example adding a "SendEmail" node:

```csharp
// 1. Add to NodeType enum
public enum NodeType
{
    // ...existing...
    SendEmail,
}

// 2. Create configuration class
public sealed class SendEmailNodeConfiguration : NodeConfiguration
{
    public override NodeType Type => NodeType.SendEmail;

    [JsonPropertyName("to")]
    [ConfigurableField(Label = "To", ControlType = ControlType.Text)]
    [SupportVariables]
    public required string To { get; init; }

    [JsonPropertyName("subject")]
    [ConfigurableField(Label = "Subject", ControlType = ControlType.Text)]
    [SupportVariables]
    public required string Subject { get; init; }

    [JsonPropertyName("body")]
    [ConfigurableField(Label = "Body", ControlType = ControlType.Code)]
    [SupportVariables]
    public required string Body { get; init; }
}

// 3. Add provider method
[NodeProvider]
public class EmailNodeProvider
{
    [NodeMethod(NodeType.SendEmail)]
    public async Task<SendEmailOutput> ExecuteSendEmailAsync(
        SendEmailNodeConfiguration config,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

The schema generator auto-discovers the config class and generates frontend-friendly schemas from attributes.

---

## Core Nodes

### Start Node

**NodeType**: `Start`

**Purpose**: Entry point, validates input against agent's InputSchema

**Configuration**:
```csharp
public sealed class StartNodeConfiguration : NodeConfiguration
{
    public override NodeType Type => NodeType.Start;

    [JsonPropertyName("inputSchema")]
    [ConfigurableField(Label = "Input Schema", ControlType = ControlType.Json)]
    public required JsonElement InputSchema { get; init; }
}
```

**Behavior**:
1. Receives execution input
2. Validates against InputSchema (JSON Schema validation)
3. Fails execution if validation fails
4. Passes input to downstream node(s) via edges

### End Node

**NodeType**: `End`

**Purpose**: Signals execution completion and returns output

**Configuration**:
```csharp
public sealed class EndNodeConfiguration : NodeConfiguration
{
    public override NodeType Type => NodeType.End;

    [JsonPropertyName("outputSchema")]
    [ConfigurableField(Label = "Output Schema", ControlType = ControlType.Json)]
    public JsonElement? OutputSchema { get; init; }
}
```

**Behavior**:
1. Receives output from upstream node(s)
2. Writes final output to execution record
3. Signals execution completion

### Model Node

**NodeType**: `Model`

**Purpose**: Call an LLM via the Providers module

**Configuration**:
```csharp
public sealed class ModelNodeConfiguration : NodeConfiguration
{
    public override NodeType Type => NodeType.Model;

    [JsonPropertyName("provider")]
    [ConfigurableField(Label = "Provider", ControlType = ControlType.Select)]
    [Tab("Basic")]
    public required LlmProvider Provider { get; init; }

    [JsonPropertyName("modelId")]
    [ConfigurableField(Label = "Model", ControlType = ControlType.Select)]
    [Tab("Basic")]
    public required string ModelId { get; init; }

    [JsonPropertyName("credentialId")]
    [ConfigurableField(Label = "Credential", ControlType = ControlType.Credential)]
    [Tab("Basic")]
    public required Guid CredentialId { get; init; }

    [JsonPropertyName("systemPrompts")]
    [ConfigurableField(Label = "System Prompts", ControlType = ControlType.TextAreaList)]
    [Tab("Prompts")]
    [SupportVariables]
    public List<string>? SystemPrompts { get; init; }

    [JsonPropertyName("userMessages")]
    [ConfigurableField(Label = "User Messages", ControlType = ControlType.TextAreaList)]
    [Tab("Prompts")]
    [SupportVariables]
    public required List<string> UserMessages { get; init; }

    [JsonPropertyName("temperature")]
    [ConfigurableField(Label = "Temperature", ControlType = ControlType.Slider)]
    [Tab("Advanced")]
    [Slider(Min = 0, Max = 2, Step = 0.1, Default = 1.0)]
    public double? Temperature { get; init; }
}
```

**Scriban Template Variables**:
- `Input` - the execution input (validated JSON matching InputSchema)
- `Steps` - dictionary of previous node outputs, keyed by node **name**

### HttpRequest Node

**NodeType**: `HttpRequest`

**Purpose**: Make HTTP requests to external APIs

**Configuration**:
```csharp
public sealed class HttpRequestNodeConfiguration : NodeConfiguration
{
    public override NodeType Type => NodeType.HttpRequest;

    [JsonPropertyName("method")]
    [ConfigurableField(Label = "Method", ControlType = ControlType.Select)]
    public required string Method { get; init; }

    [JsonPropertyName("url")]
    [ConfigurableField(Label = "URL", ControlType = ControlType.Text)]
    [SupportVariables]
    public required string Url { get; init; }

    [JsonPropertyName("headers")]
    [ConfigurableField(Label = "Headers", ControlType = ControlType.KeyValueList)]
    public KeyValueCollection? Headers { get; init; }

    [JsonPropertyName("body")]
    [ConfigurableField(Label = "Body", ControlType = ControlType.Code)]
    [SupportVariables]
    public string? Body { get; init; }

    [JsonPropertyName("timeoutSeconds")]
    [ConfigurableField(Label = "Timeout (seconds)", ControlType = ControlType.Slider)]
    [Slider(Min = 1, Max = 300, Step = 1, Default = 30)]
    public int TimeoutSeconds { get; init; } = 30;
}
```

### Sleep Node

**NodeType**: `Sleep`

**Purpose**: Pause execution for a specified duration

**Configuration**:
```csharp
public sealed class SleepNodeConfiguration : NodeConfiguration
{
    public override NodeType Type => NodeType.Sleep;

    [JsonPropertyName("durationMs")]
    [ConfigurableField(Label = "Duration (ms)", ControlType = ControlType.Number)]
    public required int DurationMs { get; init; }
}
```

### MessageFormatter Node

**NodeType**: `MessageFormatter`

**Purpose**: Format messages using Scriban templates

**Configuration**:
```csharp
public sealed class MessageFormatterNodeConfiguration : NodeConfiguration
{
    public override NodeType Type => NodeType.MessageFormatter;

    [JsonPropertyName("template")]
    [ConfigurableField(Label = "Template", ControlType = ControlType.Code)]
    [SupportVariables]
    public required string Template { get; init; }
}
```

---

## Schema-Driven UI

The frontend uses schemas generated from configuration class attributes to render property panels.

### ControlType Enum

```csharp
public enum ControlType
{
    Text,           // Single line input
    TextArea,       // Multi-line input
    TextAreaList,   // Array of text areas (SystemPrompts[], UserMessages[])
    Number,         // Numeric input
    Slider,         // Slider with min/max/step
    Select,         // Dropdown select
    Toggle,         // Boolean toggle
    Code,           // Monaco editor with Scriban support
    Json,           // Monaco editor with JSON
    KeyValueList,   // Key-value pair editor
    Credential      // Credential picker
}
```

### API Endpoint

```
GET /api/v1/nodes/{nodeType}/schema
```

Returns the schema for a node type, generated from configuration class attributes:

```json
{
  "nodeType": "HttpRequest",
  "tabs": [
    { "name": "General", "order": 0 }
  ],
  "fields": [
    {
      "name": "url",
      "label": "URL",
      "controlType": "Text",
      "tab": "General",
      "required": true,
      "supportsVariables": true,
      "order": 100
    }
  ]
}
```

---

## Orchestrator

### Responsibilities

1. Create execution record in database
2. Execute nodes in topological order (following edges)
3. Maintain execution context (node results keyed by node name)
4. Write events to RabbitMQ Stream
5. Handle all exceptions (CANNOT throw to caller)
6. Enforce execution timeout (20 minutes)
7. Update execution status throughout lifecycle

### Execution Flow

```
1. Create AgentExecution record (Status: Pending)
2. Create RabbitMQ Stream: "execution-{executionId}"
3. Emit ExecutionStarted event
4. Set Status: Running
5. Find start node - node with type "start"
6. For each node in topological order:
   a. Create NodeExecution record (Status: Running)
   b. Emit NodeStarted event
   c. Gather input from upstream nodes via context
   d. Execute node (via GenericNodeExecutor or dedicated executor)
   e. Stream any TokenDelta events (for Model nodes)
   f. Store output in context[nodeName]
   g. Update NodeExecution (Status: Completed, Output, Duration, etc.)
   h. Emit NodeCompleted event
7. Write final output to AgentExecution
8. Set Status: Completed
9. Emit ExecutionCompleted event
```

---

## Streaming Infrastructure

### RabbitMQ Streams

- **Library**: `RabbitMQ.Stream.Client`
- **One stream per execution**: Named `execution-{executionId}`
- **No partitions** (simple stream)
- **Retention**: 24 hours, then deleted by background cleanup job

### Stream Events

```csharp
abstract record ExecutionEvent(Guid ExecutionId, DateTimeOffset Timestamp);

record ExecutionStartedEvent(...) : ExecutionEvent;
record NodeStartedEvent(...) : ExecutionEvent;
record TokenDeltaEvent(...) : ExecutionEvent;
record NodeCompletedEvent(...) : ExecutionEvent;
record ExecutionCompletedEvent(...) : ExecutionEvent;
record ExecutionFailedEvent(...) : ExecutionEvent;
```

---

## Controller / API

### Execution Endpoints

**Production execution** (uses latest published version):
```
POST /api/v1/agents/{agentId}/execute
```

**Playground execution** (uses draft, falls back to latest published):
```
POST /api/v1/agents/{agentId}/test
```

### Node Schema Endpoint

```
GET /api/v1/nodes                    - List available node types
GET /api/v1/nodes/{nodeType}/schema  - Get schema for a node type
```

### Other Endpoints

```
GET  /api/v1/agents                           - List user's agents
POST /api/v1/agents                           - Create agent
GET  /api/v1/agents/{id}                      - Get agent with current version
PUT  /api/v1/agents/{id}                      - Update agent metadata
DELETE /api/v1/agents/{id}                    - Delete agent and all versions

GET  /api/v1/agents/{id}/versions             - List versions
GET  /api/v1/agents/{id}/versions/{versionId} - Get specific version
POST /api/v1/agents/{id}/versions             - Create new draft (or update existing draft)
POST /api/v1/agents/{id}/publish              - Publish current draft

GET  /api/v1/agents/executions/{id}           - Get execution status and result
GET  /api/v1/agents/executions/{id}/stream    - Reconnect to execution stream (owner only)
GET  /api/v1/agents/{id}/executions           - List executions for an agent
```

---

## Configuration

### AgentsOptions

```csharp
public class AgentsOptions
{
    public const string SectionName = "Agents";

    [Required]
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(20);

    [Required]
    public TimeSpan StreamRetention { get; set; } = TimeSpan.FromHours(24);
}
```

---

## Validation

### On Agent Save

**ReactFlow validation**:
1. ReactFlowData is valid JSON with nodes and edges arrays
2. All edges reference existing node IDs
3. Exactly one node with type "start" exists
4. Exactly one node with type "end" exists
5. Graph is connected (all nodes reachable from Start)
6. No cycles (DAG validation)

**NodeConfigurations validation**:
7. Every node ID in ReactFlowData has a corresponding entry in NodeConfigurations
8. All node names are unique within the version
9. Node names match format: lowercase `a-z`, `0-9`, `-`, `_` only
10. Each node's configuration matches its type schema
11. Credential references validated fully

### On Execution

1. Input validates against version's InputSchema
2. Version exists and user has access
3. All credentials still exist and can be decrypted

---

## Future Considerations

### Tool Calling (Post-MVP)
- Model node configuration includes tool definitions
- Tool definitions reference callable functions/agents
- Model node loops: call model -> check for tool calls -> execute tools -> call model again

### Nested Agents (Post-MVP)
- Agent can be called as a tool from another agent
- Need cycle detection to prevent Agent A -> Agent B -> Agent A

### Human-in-the-Loop (Post-MVP)
- Approval node type that pauses execution
- Writes approval request to stream
- Checkpoints state to database
- Resumes from checkpoint when approval received

### Parallel Execution (Post-MVP)
- When a node has multiple downstream connections, execute in parallel
- Join/merge semantics for converging branches
