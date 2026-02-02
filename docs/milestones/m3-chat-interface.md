# M3: Chat Interface

## Overview

Implement the chat interface for orchestrations, enabling multi-turn conversations with memory, multimodal messages, and tool usage tracking.

## Goals

1. Create conversation and message data model
2. Implement canonical message format (provider-agnostic)
3. Per-message orchestration execution with history injection
4. Tool call summaries for UI display
5. Streaming responses

## Deliverables

### Data Model

- [ ] `ConversationEntity` — conversation metadata
- [ ] `ConversationMessageEntity` — individual messages
- [ ] EF configurations for new entities
- [ ] Repository interfaces and implementations

### Canonical Message Format

- [ ] `ContentPart` base class (polymorphic)
- [ ] `TextContentPart`
- [ ] `ImageContentPart` (post-MVP)
- [ ] `AudioContentPart` (post-MVP)
- [ ] `FileContentPart` (post-MVP)
- [ ] `ToolUseContentPart` (post-MVP)
- [ ] `ToolResultContentPart` (post-MVP)
- [ ] JSON serialization with type discriminator

### Conversation Service

- [ ] `IConversationService` interface
- [ ] Create conversation
- [ ] List conversations (paginated)
- [ ] Get conversation with messages
- [ ] Delete conversation
- [ ] Send message (triggers orchestration)

### Chat Execution Flow

- [ ] Load conversation history
- [ ] Inject history into execution context
- [ ] Execute orchestration (per-message)
- [ ] Save assistant response to conversation
- [ ] Track tool call summaries
- [ ] Record token usage and model info

### Tool Integration (Post-MVP)

- [ ] Tasks tool integration
- [ ] Milestones tool integration
- [ ] Notes tool integration
- [ ] `ToolCallSummary` model for UI display

### API Endpoints

- [ ] `GET /api/v1/conversations` — list user's conversations (paginated, newest first)
- [ ] `POST /api/v1/conversations` — create conversation
- [ ] `GET /api/v1/conversations/{id}` — get conversation with all messages
- [ ] `PATCH /api/v1/conversations/{id}` — rename conversation title
- [ ] `DELETE /api/v1/conversations/{id}` — delete conversation (cascades)
- [ ] `POST /api/v1/conversations/{id}/messages` — send message (SSE stream response)
- [ ] `DELETE /api/v1/conversations/{id}/messages/{messageId}` — delete individual message

### Streaming

- [ ] SSE endpoint for streaming responses
- [ ] Token delta events during generation
- [ ] Use existing RabbitMQ stream infrastructure

## Data Model

```
Conversation
├── Id (Guid)
├── OrchestrationId (Guid) — must have Chat interface enabled
├── UserId (Guid)
├── Title (string)
├── CreatedAt (DateTimeOffset)
├── UpdatedAt (DateTimeOffset)
└── Messages (ICollection<ConversationMessageEntity>)

ConversationMessage
├── Id (Guid)
├── ConversationId (Guid)
├── Role (enum: User, Assistant, System)
├── Content (ContentPart[]) — stored as JSONB
├── ToolCallSummaries (ToolCallSummary[]) — stored as JSONB
├── InputTokens (int?)
├── OutputTokens (int?)
├── TotalTokens (int?)
├── Provider (string?)
├── Model (string?)
├── CreatedAt (DateTimeOffset)
└── Metadata (JsonDocument?)
```

## Canonical Message Format

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextContentPart), "text")]
[JsonDerivedType(typeof(ImageContentPart), "image")]
[JsonDerivedType(typeof(AudioContentPart), "audio")]
[JsonDerivedType(typeof(FileContentPart), "file")]
[JsonDerivedType(typeof(ToolUseContentPart), "tool_use")]
[JsonDerivedType(typeof(ToolResultContentPart), "tool_result")]
public abstract class ContentPart
{
    public abstract string Type { get; }
}

public class TextContentPart : ContentPart
{
    public override string Type => "text";
    public required string Text { get; set; }
}

public class ImageContentPart : ContentPart
{
    public override string Type => "image";
    public string? Base64Data { get; set; }
    public string? Url { get; set; }
    public required string MediaType { get; set; }
}

// ... other content parts
```

## Tool Call Summary

```csharp
public class ToolCallSummary
{
    public required string ToolName { get; set; }
    public required string Description { get; set; }
    public bool Success { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```

## Execution Flow

```
POST /api/v1/conversations/{id}/messages
Body: { "content": [{ "type": "text", "text": "Hello" }] }
                │
                ▼
┌──────────────────────────────────────┐
│  1. Load conversation + history      │
└──────────────────┬───────────────────┘
                   │
                   ▼
┌──────────────────────────────────────┐
│  2. Save user message to DB          │
└──────────────────┬───────────────────┘
                   │
                   ▼
┌──────────────────────────────────────┐
│  3. Build execution input            │
│     - Current message                │
│     - Conversation history           │
│     - Available tools                │
└──────────────────┬───────────────────┘
                   │
                   ▼
┌──────────────────────────────────────┐
│  4. Execute orchestration            │
│     ExecutionInterface = Chat        │
└──────────────────┬───────────────────┘
                   │
                   ▼
┌──────────────────────────────────────┐
│  5. Stream response via SSE          │
│     - TokenDelta events              │
│     - Tool call events               │
└──────────────────┬───────────────────┘
                   │
                   ▼
┌──────────────────────────────────────┐
│  6. Save assistant message           │
│     - Response content               │
│     - Tool call summaries            │
│     - Token usage                    │
└──────────────────────────────────────┘
```

## API Models

### CreateConversationRequestV1

```csharp
public class CreateConversationRequestV1
{
    public required Guid OrchestrationId { get; set; }
    public string? Title { get; set; }
}
```

### SendMessageRequestV1

```csharp
public class SendMessageRequestV1
{
    public required List<ContentPart> Content { get; set; }
}
```

### ConversationResponseV1

```csharp
public class ConversationResponseV1
{
    public Guid Id { get; set; }
    public Guid OrchestrationId { get; set; }
    public string? Title { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<ConversationMessageV1> Messages { get; set; }
}
```

## MVP Scope

| Feature | MVP | Later |
|---------|-----|-------|
| Multi-turn conversations | ✓ | |
| Text messages | ✓ | |
| Image messages | | ✓ |
| Audio messages | | ✓ |
| File/document messages | | ✓ |
| Server-side tools | | ✓ |
| User orchestrations as tools | | ✓ |
| Tool call summaries | | ✓ |
| Streaming responses | ✓ | |
| Conversation export | | ✓ |
| Conversation branching | | ✓ |
| Context window management | | ✓ |
| Concurrent message locking | | ✓ |

## Dependencies

- M1: Orchestration Rename (interfaces schema, ExecutionInterface)
- M2: MCP Server Native Tools (for server-side tools)
- Existing orchestration execution engine
- Existing SSE/RabbitMQ streaming infrastructure

## Design Decisions

### Input Schema Not Required for Chat

`InputSchema` on `OrchestrationVersion` becomes **optional**. Only required when certain interfaces are enabled:

| Interface | InputSchema Required | Reason |
|-----------|---------------------|--------|
| MCP | ✓ | Defines tool parameters |
| Direct | ✓ | Structured API validation |
| A2A | ✗ | A2A message format is the standard |
| Chat | ✗ | `ContentPart[]` is implicit |
| Webhook | ✗ | Raw payload, no validation |

Chat input is always a user message with `ContentPart[]` (text, images, audio) - no schema validation needed.

### Conversation History Loading

History is loaded at the **Start node**, not the Model node:

1. Start node detects `ExecutionInterface == Chat`
2. Loads conversation history into `ExecutionContext.ConversationHistory`
3. Model node decides whether to **use** the already-loaded history

### Model Node Configuration for Chat

Model node gets two new checkboxes:

```csharp
public class ModelNodeConfiguration
{
    // ... existing fields
    public bool LoadConversationHistory { get; set; }  // Include history in LLM request
    public bool UseInput { get; set; }                  // Include current input as user message
}
```

- `LoadConversationHistory` → append `context.ConversationHistory` to messages
- `UseInput` → append `context.Input` (the current user message)
- `UserMessage` field becomes optional (can be empty if using input/history)

This allows:
- Same Model node works for single-turn and multi-turn
- Chat orchestrations just check the boxes
- Flexibility to ignore input or history for specific use cases (e.g., summarization)

### End Node Saves to Conversation History

When `ExecutionInterface == Chat`, the End node saves the output to conversation history:

1. Captures the assistant response (from execution output)
2. Collects tool call summaries from execution context
3. Records token usage and model info
4. Saves as new `ConversationMessage` with role `Assistant`

```
End Node (Chat mode)
├── Capture output as ContentPart[]
├── Collect ToolCallSummary[] from context
├── Record InputTokens, OutputTokens, Provider, Model
└── Save ConversationMessage to DB
```

The user message is saved at the **start** of execution (before orchestration runs), the assistant message is saved at the **end** (after orchestration completes). This ensures partial responses are captured even if execution fails mid-way.

### Streaming Behavior

Streaming is determined by **graph topology**, not configuration:

1. **Only models connected to End stream** — intermediate models execute silently
2. **Multiple models connected to End** — all stream, frontend distinguishes by `nodeId` in SSE events
3. **End node saves all inputs** — multiple model outputs become multiple `ContentPart` entries in the assistant message

**Single model:**
```
Start → Model → End
                 ↓
         ContentPart[TextContentPart]
```

**Model chain (only terminal streams):**
```
Start → Model A → Model B → End
           ↓          ↓
       (silent)   (streams)
```

**Parallel models (both stream):**
```
Start → Model A ─┬→ End
      → Model B ─┘    ↓
              ContentPart[TextA, TextB]
```

**SSE events vs saved content:**
- SSE events include `nodeId` for realtime differentiation
- Saved `ContentPart[]` is just content (no nodeId) — can extend later if needed

### File and Image Handling

Images and files are managed through a separate Files system, not inline in messages.

**User uploads (separate from chat):**
1. User uploads file → backend stores in SeaweedFS (blob storage)
2. Creates `File` record: GUID, filename, mime type, storage path
3. User has "Files" section to manage their uploads

**Sending images in chat:**
1. User attaches image → sends `ImageContentPart` with just the file GUID
2. Start node hydrates file references → loads actual bytes from SeaweedFS
3. Model receives canonical format with actual image data

**Model-generated images:**
1. Model returns image content
2. Backend stores in SeaweedFS → creates `File` record for user
3. Returns `ImageContentPart` with GUID, mime type, filename
4. Frontend renders via file endpoint

**ImageContentPart format (stored in conversation):**
```json
{
  "type": "image",
  "fileId": "guid-here",
  "mimeType": "image/png",
  "fileName": "chart.png"
}
```

**Benefits:**
- DB stores GUIDs, not huge base64 blobs
- Files reusable across conversations
- Same pattern for input and output images
- User owns their files (visible in Files section)
- Frontend fetches via `/api/v1/files/{id}/content`

### Message Structure (Nested Polymorphism)

```
Message
├── Role (user | assistant | system)
└── Content: ContentPart[]  ← polymorphic array
    ├── TextContentPart
    ├── ImageContentPart
    ├── AudioContentPart
    ├── ToolUseContentPart
    └── ToolResultContentPart
```

A single user message can contain multiple content parts (text + images + audio).

---

## MVP Decisions

### Content Parts
- **MVP: TextContentPart only** — images, audio, tools all post-MVP
- Build up from simple text, extend content part types later

### Tools
- **No tools for MVP** — tool integration deferred to post-MVP

### Audio Handling
- Same pattern as images: upload → SeaweedFS → GUID → hydrate at Start

### Error Handling
- If orchestration fails mid-execution: user message saved, no assistant message
- User can retry (frontend handles as: delete messages → re-send)

### Conversation Management

| Feature | Decision |
|---------|----------|
| Titles | `Conversation_1`, `Conversation_2`, etc. (revisit later for smart titles) |
| Update | Rename title only |
| Delete conversation | Hard delete, cascades to messages |
| Delete message | Cherry-pick individual messages (user's responsibility if context breaks) |
| List | Newest to oldest, orchestration name shown as field |
| Pagination | Default 20 per page |
| Get conversation | Returns all messages (files are just GUIDs, lightweight) |
| Ownership | User isolation via DbContext filter |
| Max message size | No limits for MVP |
| Concurrent messages | Post-MVP, will use global lock on conversation ID |

### Orchestration Rules

| Rule | Behavior |
|------|----------|
| Chat interface required | UI shows agent cards for chat-enabled orchestrations only |
| Orchestration version | Conversation uses latest published version |
| Orchestration deleted | Conversations become read-only |
| Orchestration unpublished | Chat requires published version (playground can test drafts) |
| Chat disabled mid-conversation | Undefined for MVP |

### API Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /api/v1/conversations` | List user's conversations (paginated, newest first) |
| `POST /api/v1/conversations` | Create conversation (requires orchestration ID) |
| `GET /api/v1/conversations/{id}` | Get conversation with all messages |
| `PATCH /api/v1/conversations/{id}` | Rename conversation title |
| `DELETE /api/v1/conversations/{id}` | Delete conversation (cascades) |
| `POST /api/v1/conversations/{id}/messages` | Send message (triggers orchestration) |
| `DELETE /api/v1/conversations/{id}/messages/{messageId}` | Delete individual message |

### SSE Event Types

```
response_start
├── part_start { partType: "text" }
│   ├── part_delta { content: "..." }
│   └── part_end
├── token_usage { input: N, output: N }
├── response_error { error: "..." }  // if error occurs
└── response_end
```

| Event | Description |
|-------|-------------|
| `response_start` | Beginning of assistant response |
| `part_start` | Beginning of content part (includes type) |
| `part_delta` | Streaming content (text, thinking, etc.) |
| `part_end` | End of content part |
| `token_usage` | Token counts |
| `response_error` | Error during stream |
| `response_end` | End of response |

### Streaming Behavior
- SSE only, no polling
- No reconnection — if disconnected, refresh to pull saved state
- Node ID in events for multi-model differentiation

### Messages
- Immutable once saved (just `CreatedAt`, no `UpdatedAt`)
- User messages on right, assistant on left (frontend)
- Assistant message: array of content parts

---

## Open Questions (Deferred)

1. **Context window management** — How to handle long conversations exceeding model limits? (sliding window, summarization, etc.)
2. **Welcome message** — Use `ChatInterfaceConfig.WelcomeMessage` as placeholder text or UI hint?
3. **Smart titles** — Auto-generate from first message?
