# Agents Module

Core agentic framework containing the orchestrator, agent execution, and node system.

## Overview

- **Frontend**: ReactFlow for visual workflow editing
- **Triggers**: API call or playground only (MVP). Webhook and scheduled triggers planned for later.
- **Streaming**: RabbitMQ Streams for event delivery, SSE for client consumption
- **Templating**: Scriban for dynamic message construction

---

## MVP Scope

### In Scope
- Start, Model (text only), End nodes
- Sequential execution (single path)
- RabbitMQ Streams for event streaming
- SSE and non-streaming API responses
- JSON Schema validation for input/output
- Scriban templating for Model node messages
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

**Note**: InputSchema and OutputSchema are on AgentVersion, not Agent, allowing schemas to evolve between versions.

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
  - `type`: node type string ("start", "model", "end")
  - `position`: { x, y }
  - `data`: frontend-specific data (labels, etc.)
- `edges[]`: array of edge objects
  - `id`: GUID string
  - `source`: source node ID
  - `target`: target node ID
- `viewport`: zoom/pan state (frontend only)

**The orchestrator uses ReactFlowData.edges** to compute the graph structure, determining which nodes connect to which. Node inputs/outputs are linked via these edges during execution.

**NodeConfigurations**: Backend's source of truth for execution. Dictionary keyed by node GUID, containing typed configuration per node type. Each configuration includes a `name` field for template references.

**Node Naming**:
- Each node has a unique `name` in its configuration
- Default name is `{type}_{n}` (e.g., "start_1", "model_1", "model_2", "end_1")
- Users can rename nodes in the UI
- Internal name format: lowercase, spaces replaced with `_`, allowed chars: `a-z`, `0-9`, `-`, `_`
- Names must be unique within an agent version (validated on save)
- Scriban templates reference nodes by name: `{{ steps.model_1 }}` not by GUID

**Versioning Rules:**
- Each agent has at most ONE draft version at a time
- Editing always updates the same draft until published
- Publishing creates a new version number, sets IsDraft=false, sets PublishedAt
- Playground execution (`/test`): uses draft if exists, otherwise latest published
- Production execution (`/execute`): uses latest published by default, or specific version if requested

**Default Agent Template** (created on `POST /agents`):
- Draft version with Start → End nodes connected
- InputSchema: `{ "type": "object", "properties": { "input": { "type": "string" } }, "required": ["input"] }`
- OutputSchema: null (no validation)
- User drags additional nodes onto the canvas from there

### Credential Mapping

```
AgentVersionCredentialMapping
├── Id (UUID)
├── AgentVersionId (UUID) - FK to AgentVersion
├── NodeId (string) - the node ID (GUID) that references this credential
├── CredentialId (UUID) - FK to ExternalApiKey
```

**Credential Rules:**
- Block credential deletion only if referenced by current published version OR draft
- Old versions can have dangling credential references (execution will fail)
- Credentials resolved at runtime by calling Credentials module to decrypt
- **Full validation on save**: verify credential exists, user owns it, AND can be decrypted

### Execution

```
AgentExecution
├── Id (UUID)
├── UserId (UUID)
├── AgentId (UUID) - FK to Agent
├── AgentVersionId (UUID) - FK to AgentVersion
├── Status (enum: Pending, Running, Completed, Failed, Cancelled)
├── Input (JSONB)
├── Output (JSONB, nullable)
├── ErrorMessage (string, nullable)
├── StartedAt (DateTimeOffset)
├── CompletedAt (DateTimeOffset, nullable)
├── TotalTokensUsed (int, nullable)
├── StreamName (string) - RabbitMQ stream name for this execution
└── NodeExecutions[] - navigation property
```

**Execution History**: Kept forever, no automatic cleanup.

### Node Execution (Full Trace)

```
AgentNodeExecution
├── Id (UUID)
├── AgentExecutionId (UUID) - FK to AgentExecution
├── NodeId (string) - GUID, matches key in NodeConfigurations
├── NodeType (string) - "start", "model", "end"
├── Status (enum: Pending, Running, Completed, Failed)
├── Input (JSONB, nullable)
├── Output (JSONB, nullable)
├── ErrorMessage (string, nullable)
├── StartedAt (DateTimeOffset)
├── CompletedAt (DateTimeOffset, nullable)
├── TokensUsed (int, nullable) - for Model nodes
├── FullResponse (text, nullable) - complete LLM response for Model nodes
└── DurationMs (int, nullable)
```

---

## Node Types

Nodes are registered via DI using a registry pattern, allowing new node types to be added.

Node type is determined by the `type` field in ReactFlow node data. The orchestrator uses this to look up the appropriate executor from the registry.

### Node Executor Base Class

Node executors inherit from a base class that handles common orchestration concerns:

```csharp
public abstract class NodeExecutor<TConfig, TOutput> where TOutput : NodeOutput
{
    // Base class handles:
    // - Emitting NodeStarted event
    // - Timing/duration tracking
    // - Exception handling and error events
    // - Emitting NodeCompleted event
    // - Storing output in context

    protected abstract Task<TOutput> ExecuteInternalAsync(
        TConfig config,
        ExecutionContext context,
        CancellationToken cancellationToken);
}
```

Concrete node types (StartNodeExecutor, ModelNodeExecutor, EndNodeExecutor) implement `ExecuteInternalAsync` with their specific logic. The base class wraps execution with stream notifications.

### Node Output Base Class

All node outputs derive from a common base class:

```csharp
public abstract class NodeOutput
{
    /// <summary>
    /// Converts the output to a string suitable for LLM message content.
    /// Override in derived classes to provide custom formatting.
    /// </summary>
    public virtual string ToMessageOutput() => ToString();
}
```

When joining multiple upstream outputs (future parallel execution), outputs are joined via their `ToMessageOutput()` representations.

### Start Node

**ReactFlow type**: `"start"`

**Purpose**: Entry point, validates input against agent's InputSchema

**Configuration**:
```json
{
  "name": "start_1"
}
```

| Field | Required | Description |
|-------|----------|-------------|
| name | Yes | Unique node name (typically "start_1") |

**Behavior**:
1. Receives execution input
2. Validates against version's InputSchema (JSON Schema validation)
3. Fails execution if validation fails
4. Passes input to downstream node(s) via edges

**Output**: `StartNodeOutput` - wraps the validated input JSON

### Model Node

**ReactFlow type**: `"model"`

**Purpose**: Call an LLM via the Providers module

**Configuration** (all fields validated on save):
```json
{
  "name": "model_1",
  "provider": "OpenAI",
  "modelId": "gpt-4o",
  "credentialId": "uuid",
  "systemPrompt": "You are a helpful assistant. Context: {{ input.context }}",
  "userMessage": "{{ input.query }}",
  "temperature": 0.7,
  "maxTokens": 4096,
  "topP": 1.0
}
```

| Field | Required | Description |
|-------|----------|-------------|
| name | Yes | Unique node name for template references |
| provider | Yes | LlmProvider enum value |
| modelId | Yes | Model identifier string |
| credentialId | Yes | FK to ExternalApiKey - **must exist and be decryptable** |
| systemPrompt | No | Scriban template for system message |
| userMessage | Yes | Scriban template for user message |
| temperature | No | Provider default if not set |
| maxTokens | No | Provider default if not set |
| topP | No | Provider default if not set |

**Scriban Template Variables**:
- `input` - the execution input (validated JSON matching InputSchema). Fields defined in InputSchema are accessible (e.g., `{{ input.query }}`, `{{ input.context }}`)
- `steps` - dictionary of previous node outputs, keyed by node **name**

Example templates:
```
systemPrompt: "You are a {{ input.role }}. Always respond in {{ input.language }}."
userMessage: "Based on this context: {{ steps.summarizer.output }}\n\nAnswer: {{ input.question }}"
```

**Template Error Handling**: If a template references a step name that doesn't exist, the node throws an error and the execution fails.

**Behavior**:
1. Render systemPrompt template (if configured) with Scriban
2. Render userMessage template with Scriban
3. Resolve credential at runtime (call Credentials module to decrypt API key)
4. Call Providers module `IModelPipeline.ExecuteAsync()`
5. Stream TokenDelta events to execution stream
6. Accumulate full response
7. Store full response and token usage in NodeExecution record

**Output**: `ModelNodeOutput` - contains the complete LLM response text (and token usage)

### End Node

**ReactFlow type**: `"end"`

**Purpose**: Signals execution completion and returns output

**Configuration**:
```json
{
  "name": "end_1"
}
```

| Field | Required | Description |
|-------|----------|-------------|
| name | Yes | Unique node name (typically "end_1") |

**Behavior (MVP)**:
1. Receives output from upstream node(s) connected via edges
2. For single upstream: uses that output directly
3. For multiple upstream (future): joins via `ToMessageOutput()` method
4. Writes final output to execution record
5. Signals execution completion

**Output**: `EndNodeOutput` - wraps the final result

**Future**: Schema mapping to extract/transform fields from context; OutputSchema validation

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
   d. Execute node
   e. Stream any TokenDelta events (for Model nodes)
   f. Store output in context[nodeName]
   g. Update NodeExecution (Status: Completed, Output, Duration, etc.)
   h. Emit NodeCompleted event
7. Write final output to AgentExecution
8. Set Status: Completed
9. Emit ExecutionCompleted event
```

### Error Handling

- All exceptions caught and written to stream as ExecutionFailed event
- Node failures: mark node as Failed, mark execution as Failed
- Timeout: cancel execution, mark as Failed with timeout error
- Never throw - always emit error event to stream

### Context

Node results stored in a dictionary keyed by node **name**:
```csharp
Dictionary<string, object> Context
// e.g., Context["start_1"] = { ... validated input ... }
//       Context["model_1"] = "LLM response text..."
//       Context["end_1"] = "final output"
```

This context is exposed to Scriban templates as the `steps` variable. Templates access values like `{{ steps.model_1 }}` or `{{ steps.model_1.some_field }}`.

---

## Streaming Infrastructure

### RabbitMQ Streams

- **Library**: `RabbitMQ.Stream.Client`
- **One stream per execution**: Named `execution-{executionId}`
- **No partitions** (simple stream)
- **Retention**: 24 hours, then deleted by background cleanup job

### Stream Events (SSE-compatible format)

All events serialized as JSON, compatible with SSE `data:` field.

```csharp
abstract record ExecutionEvent(Guid ExecutionId, DateTimeOffset Timestamp);

record ExecutionStartedEvent(...) : ExecutionEvent;
record NodeStartedEvent(...) : ExecutionEvent;
record TokenDeltaEvent(...) : ExecutionEvent;
record NodeCompletedEvent(...) : ExecutionEvent;
record ExecutionCompletedEvent(...) : ExecutionEvent;
record ExecutionFailedEvent(...) : ExecutionEvent;
```

**JSON Serialization**: Use `type` field as discriminator (not `$type`):
```json
{
  "type": "NodeStarted",
  "executionId": "...",
  "timestamp": "...",
  "nodeId": "...",
  "nodeType": "model"
}
```

### Stream Lifecycle

1. **Creation**: Controller creates stream before starting orchestrator
2. **Writing**: Orchestrator writes events during execution
3. **Reading**: Controller consumes stream, yields as SSE
4. **Cleanup**: Background job deletes streams older than 24 hours

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

**Request body** (same for both):
```json
{
  "input": { ... },
  "versionId": "uuid"
}
```

| Field | Required | Description |
|-------|----------|-------------|
| input | Yes | Agent input, validated against InputSchema |
| versionId | No | Specific version to execute (overrides default behavior) |

**Behavior**:
1. Validate request
2. Load agent and version:
   - `/execute`: latest published, or specific versionId if provided
   - `/test`: draft if exists, else latest published, or specific versionId if provided
3. Validate input against version's InputSchema
4. Create RabbitMQ Stream
5. Instantiate orchestrator (scoped, in-process for MVP)
6. Start orchestrator execution

### Response Modes

**Streaming (Accept: text/event-stream)**:
- Return immediately with SSE stream
- Yield events as they arrive from RabbitMQ Stream
- Close stream on ExecutionCompleted or ExecutionFailed

**Non-streaming (Accept: application/json)**:
- Wait for execution to complete (up to 20 minute timeout)
- Return final output as JSON response
- Return error response if execution fails

### Reconnection

Clients can reconnect to an in-progress or completed execution:
```
GET /api/v1/agents/executions/{executionId}/stream?offset={offset}
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| offset | No | Stream offset to start from. Default: 0 (replay from beginning) |

- **Owner only**: requires authenticated user to be the execution owner
- Reads from RabbitMQ Stream starting at specified offset
- Useful if client disconnects mid-execution and wants to resume without replaying all events
- Client can track last received offset and pass it on reconnect

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

GET  /api/v1/agents/node-types                - List available node types and their config schemas
```

### Node Types Discovery

```
GET /api/v1/agents/node-types
```

Returns available node types for the frontend to render in the node palette:
```json
{
  "nodeTypes": [
    {
      "type": "start",
      "displayName": "Start",
      "description": "Entry point - validates input",
      "configSchema": { ... JSON Schema for StartNodeConfig ... }
    },
    {
      "type": "model",
      "displayName": "Model",
      "description": "Call an LLM",
      "configSchema": { ... JSON Schema for ModelNodeConfig ... }
    },
    {
      "type": "end",
      "displayName": "End",
      "description": "Output and completion",
      "configSchema": { ... JSON Schema for EndNodeConfig ... }
    }
  ]
}
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

### RabbitMQ Stream Configuration

```csharp
public class RabbitMqStreamOptions
{
    public const string SectionName = "RabbitMqStream";

    [Required]
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5552;  // Stream protocol port

    [Required]
    public string Username { get; set; } = "guest";

    [Required]
    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";
}
```

---

## Validation

### On Agent Save (Create/Update Draft)

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
10. Each node's configuration matches its type schema:
    - Start node: name required
    - Model node: name, provider, modelId, credentialId, userMessage all required
    - End node: name required
11. Credential references validated fully:
   - Valid UUID format
   - Credential exists in database
   - User owns the credential
   - Credential can be decrypted

**Schema validation**:
12. InputSchema is valid JSON Schema (required)
13. OutputSchema is valid JSON Schema (if provided)

### On Execution

1. Input validates against version's InputSchema
2. Version exists and user has access
3. All credentials still exist and can be decrypted (may have changed since save)

---

## Providers Module Integration

The Model node calls the existing Providers module for LLM execution.

**Current gap**: `PipelineModelConfig` only has Provider and ModelId, no credential/API key field.

**Resolution** (to be implemented when integrating):
1. Agents module decrypts credential via Credentials module
2. Extend `PipelineModelConfig` or `ModelPipelineRequest` to accept the decrypted API key
3. Providers module uses the provided key instead of looking one up

This is **out of scope for the Agents module MVP** - the Providers module extension will be done as a separate task when we wire up the Model node execution.

---

## Future Considerations

### Tool Calling (Post-MVP)
- Model node configuration includes tool definitions
- Tool definitions reference callable functions/agents
- Model node loops: call model -> check for tool calls -> execute tools -> call model again
- Configurable max iterations to prevent infinite loops

### Nested Agents (Post-MVP)
- Agent can be called as a tool from another agent
- Need cycle detection to prevent Agent A -> Agent B -> Agent A
- Depth limits (configurable)
- Context/credential isolation between parent and child

### Human-in-the-Loop (Post-MVP)
- Approval node type that pauses execution
- Writes approval request to stream
- Checkpoints state to database
- Resumes from checkpoint when approval received
- State machine for warm-up on resume

### Parallel Execution (Post-MVP)
- When a node has multiple downstream connections, execute in parallel
- Join/merge semantics for converging branches
- Partial failure handling

### Wolverine Integration (Post-MVP)
- Offload execution to Wolverine message handler
- Separates HTTP lifecycle from execution lifecycle
- Enables horizontal scaling of workers
- Single message type: ExecuteAgentCommand

---

## Infrastructure Requirements

### Docker Compose Additions

```yaml
rabbitmq:
  image: rabbitmq:3-management
  container_name: donkeywork-rabbitmq
  environment:
    RABBITMQ_DEFAULT_USER: guest
    RABBITMQ_DEFAULT_PASS: guest
  ports:
    - "5672:5672"    # AMQP
    - "5552:5552"    # Stream protocol
    - "15672:15672"  # Management UI
  volumes:
    - ./data/rabbitmq:/var/lib/rabbitmq
  command: >
    bash -c "rabbitmq-plugins enable rabbitmq_stream && rabbitmq-server"
```

### NuGet Packages

- `RabbitMQ.Stream.Client` - RabbitMQ Streams client
- `Scriban` - Template engine for message construction
- `JsonSchema.Net` or `NJsonSchema` - JSON Schema validation

---

## Open Questions

1. **Rate limiting**: Any rate limits on execution requests per user? (Deferred for MVP)

2. **Scriban security**: Should we sandbox Scriban to prevent arbitrary code execution? (Scriban is safe by default, but worth confirming)

## Resolved Questions

- **Output schema validation**: No validation for MVP, pass-through only
- **Missing step reference**: Node throws error, execution fails
- **Default credentials**: Not supported - credentialId is required
- **Execution history cleanup**: Keep forever, no auto-cleanup
- **Node naming**: Names required, unique, validated format (a-z, 0-9, -, _)
- **Cancellation**: Not for MVP, will use stream for cancel signals in future
- **Empty agent (Start→End)**: Valid execution, returns input as output (passthrough)
- **Concurrent draft editing**: Last-write-wins for MVP, no optimistic locking
- **Event serialization**: Polymorphic JSON with base class discriminator
