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
- [ ] `ImageContentPart`
- [ ] `AudioContentPart` (post-MVP)
- [ ] `FileContentPart` (post-MVP)
- [ ] `ToolUseContentPart`
- [ ] `ToolResultContentPart`
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

### Tool Integration (MVP: Server-Side Only)

- [ ] Tasks tool integration (todos exposed as tasks)
- [ ] Milestones tool integration
- [ ] Notes tool integration
- [ ] `ToolCallSummary` model for UI display

### API Endpoints

- [ ] `GET /api/v1/conversations` — list user's conversations
- [ ] `POST /api/v1/conversations` — create conversation
- [ ] `GET /api/v1/conversations/{id}` — get conversation with messages
- [ ] `DELETE /api/v1/conversations/{id}` — delete conversation
- [ ] `POST /api/v1/conversations/{id}/messages` — send message
- [ ] `GET /api/v1/conversations/{id}/messages/{messageId}/stream` — reconnect to stream

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
| Image messages | ✓ | |
| Audio messages | | ✓ |
| File/document messages | | ✓ |
| Server-side tools | ✓ | |
| User orchestrations as tools | | ✓ |
| Tool call summaries | ✓ | |
| Streaming responses | ✓ | |
| Conversation export | | ✓ |
| Conversation branching | | ✓ |

## Dependencies

- M1: Orchestration Rename (interfaces schema, ExecutionInterface)
- M2: MCP Server Native Tools (for server-side tools)
- Existing orchestration execution engine
- Existing SSE/RabbitMQ streaming infrastructure

## Open Questions

1. **Context window management** — How to handle long conversations exceeding model limits?
2. **History format** — How to format conversation history for the model node?
3. **Tool selection** — Which tools are available in chat? All native tools? Configurable per orchestration?
