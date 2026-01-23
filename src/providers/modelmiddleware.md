# ModelMiddleware Architecture

This document describes the middleware pipeline system for AI model execution.

## Overview

The middleware uses a **chain-of-responsibility pattern** with `IAsyncEnumerable<BaseMiddlewareMessage>` for streaming responses. Each middleware can intercept, transform, or pass through messages.

The middleware can support re-entrancy in the tooling, or other middlewares, by simply calling `next` again.

The service that creates the middleware pipeline should be **transient** to ensure fresh pipeline instances per request. 

---

## Core Interface

```csharp
// Middleware/IModelMiddleware.cs
public interface IModelMiddleware
{
    IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next);
}
```

- `context`: Request data, messages, tool config, guardrails config
- `next`: Delegate to call the next middleware in the chain
- Returns: Async stream of messages flowing back up the pipeline

---

## Pipeline Order

Defined in `Base/ModelPipeline.cs`:

```csharp
private static readonly Type[] PipelineOrder =
[
    typeof(BaseExceptionMiddleware),      // 1. Error boundary
    typeof(ToolMiddleware),               // 2. Tool execution loop
    typeof(GuardrailsMiddleware),         // 3. Security/compliance
    typeof(AccumulatorMiddleware),        // 4. Concatenates streamed text, thinking, tool calls into full message; appends to context.Messages
    typeof(ProviderMiddleware)            // 5. AI provider call
];
```

---

## Pipeline Construction

The pipeline is built recursively via `CreatePipelineFunc`:

```csharp
// Base/ModelPipeline.cs
private Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> CreatePipelineFunc(
    List<Type> middlewareTypes,
    int index)
{
    // Base case: end of chain
    if (index >= middlewareTypes.Count)
    {
        return _ => EmptyAsyncEnumerableAsync<BaseMiddlewareMessage>();
    }

    var middlewareType = middlewareTypes[index];

    // Resolve and cache middleware instance
    if (!this.middlewareCache.TryGetValue(middlewareType, out var middleware))
    {
        middleware = (IModelMiddleware)this.serviceProvider.GetRequiredService(middlewareType);
        this.middlewareCache[middlewareType] = middleware;
    }

    // Recursive: create delegate for remaining middleware
    var next = this.CreatePipelineFunc(middlewareTypes, index + 1);

    // Return execution function
    return context => middleware.ExecuteAsync(context, next);
}
```

Key points:
- Middleware instances are cached for reuse across tool iterations
- Delegates are created at construction time, not execution time
- Cancellation propagates via `.WithCancellation(cancellationToken)` on `IAsyncEnumerable`

---

## Input Context

```csharp
public class ModelMiddlewareContext
{
    public List<BaseMessage> Messages { get; set; }        // Conversation history (mutable for re-entrancy)
    public ModelConfig Model { get; set; }                  // LLM configuration
    public ToolContext ToolContext { get; set; }            // Tool definitions
    public Dictionary<string, object> Variables { get; }    // Middleware-shared state
    public Dictionary<string, object> ProviderParameters { get; set; }  // Provider-specific params (e.g., thinking budget)
}

// Example ProviderParameters keys (provider-specific)
// - "thinking_budget" (int) - Anthropic extended thinking budget_tokens
// - "interleaved_thinking" (bool) - Enable thinking between tool calls (Anthropic beta)
// - "reasoning_effort" (string) - OpenAI: "low", "medium", "high"
// - "reasoning_summary" (string) - OpenAI: "auto", "concise", "detailed"
// - "temperature" (float)
// - "max_tokens" (int)
// - "top_p" (float)
// Note: CancellationToken flows via [EnumeratorCancellation] on middleware methods, not via context

// Well-known variable keys
public static class MiddlewareVariables
{
    public const string MaxToolExecutions = "MaxToolExecutions";  // int, default: 20
    public const string ParallelToolCalls = "ParallelToolCalls";  // bool, default: false
    // Used by: ProviderMiddleware (tells model it can request multiple tools)
    //          ToolMiddleware (executes tools concurrently via Task.WhenAll)
}
```

**Design Decision:** `Messages` is intentionally mutable. The re-entrancy pattern (ToolMiddleware loops, AccumulatorMiddleware appends) requires middlewares to share and modify the same message list. Since the pipeline service is transient (one instance per request), there's no thread-safety concern.

### Messages

Messages should have a role (user, assistant, system).
user messages should have a mime type and content.
Content is assumed to be base64 or text depending on mime type.
user messages can contain tool responses as well, so create a polymorphic message model.
assistant messages can contain thinking, tool calls, mimetype and content for the content alike user messages.

> **TODO:** Document the full message model class hierarchy (BaseMessage, UserMessage, AssistantMessage, SystemMessage, content types, etc.)

## Response Message Types

### Hierarchy

```
BaseMiddlewareMessage (abstract)
├── ModelMiddlewareMessage
│   └── ModelMessage: ModelResponseBase
│       ├── ModelResponseBlockStart               (block lifecycle - start with index + type)
│       ├── ModelResponseBlockEnd                 (block lifecycle - end with index)
│       ├── ModelResponseTextContent              (text chunks)
│       ├── ModelResponseThinkingContent          (readable thinking chunks - can interleave)
│       ├── ModelResponseEncryptedThinkingContent (opaque encrypted reasoning - always new block)
│       ├── ModelResponseToolCall                 (client tool requests - executed by ToolMiddleware)
│       ├── ModelResponseServerToolCall           (server tool requests - executed by provider, ignored by ToolMiddleware)
│       ├── ModelResponseUsage                    (token counts)
│       ├── ModelResponseErrorContent             (errors)
│       └── ModelResponseStreamEnd                (stop reason + metadata)
├── GuardrailsMiddlewareMessage          (policy violations)
├── ModelMiddlewareToolRequestMessage    (hydrated client tool call)
└── ModelMiddlewareToolResponseMessage   (client tool execution result)
```

**Server vs Client Tools:**
- `ModelResponseToolCall` = client tools, executed locally by ToolMiddleware
- `ModelResponseServerToolCall` = server/provider tools (e.g., OpenAI web_search), already executed by provider
- ToolMiddleware ignores `ModelResponseServerToolCall` - just passes through
- Provider may yield server tool results; ignored for MVP

**Thinking Block Accumulation (AccumulatorMiddleware):**
- `ModelResponseThinkingContent` (unencrypted): if last block in accumulated message is `ThinkingBlock`, append; otherwise add new `ThinkingBlock`
- `ModelResponseEncryptedThinkingContent`: always add new `EncryptedThinkingBlock` (never append)

```csharp
public class ModelResponseThinkingContent : ModelResponseBase
{
    public string Content { get; set; }
    public string? Signature { get; set; }  // Anthropic verification signature (set on final chunk)
}

public class ModelResponseEncryptedThinkingContent : ModelResponseBase
{
    public string EncryptedContent { get; set; }  // Opaque blob (OpenAI)
    public string? Signature { get; set; }        // Optional verification
}
```

```csharp
public class ModelResponseStreamEnd : ModelResponseBase
{
    public StopReason Reason { get; set; }
    public Dictionary<string, object> Metadata { get; set; }  // Provider-specific accumulated data
}

public enum StopReason
{
    EndTurn,        // Natural completion
    ToolUse,        // Model wants to call tools
    MaxTokens,      // Hit token limit
    Incomplete,     // OpenAI: max output reached but response not finished
    ContentFilter,  // Blocked by provider safety
    SafetyStop,     // Gemini: blocked by safety settings
    Recitation,     // Gemini: blocked due to recitation concerns
    Cancelled       // Client cancellation
}

public class ModelResponseBlockStart : ModelResponseBase
{
    public int BlockIndex { get; set; }
    public ContentBlockType Type { get; set; }
}

public class ModelResponseBlockEnd : ModelResponseBase
{
    public int BlockIndex { get; set; }
}

// Content blocks = renderable output
public enum ContentBlockType
{
    Text,
    // Future (post-MVP):
    // Image,
    // Audio
}

// Note: Thinking, ToolCall, ServerToolCall have their own message types
// and may need separate lifecycle handling - TBD during testing

// Example Metadata keys (accumulated by provider during stream):
// - "thinking_signature" (string) - Anthropic thinking block signature for round-trip
// - "usage" (object) - Token usage if not yielded separately
// - "model_version" (string) - Actual model version used
// - "system_fingerprint" (string) - OpenAI system fingerprint
// - "safety_ratings" (object) - Gemini safety ratings
```

## Data Flow

```
REQUEST FLOW (down):
┌─────────────────────────────────────────────────────────────┐
│  ModelMiddlewareContext                                     │
│  - Messages (conversation history)                          │
│  - Model (LLM config)                                       │
│  - ToolContext (tool definitions)                           │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  1. BaseExceptionMiddleware                                 │
│     Wraps entire pipeline with exception handler            │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  2. ToolMiddleware                                          │
│     Loops calling next() until no tool calls                │
│     Mutates context.Messages with tool results              │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  3. GuardrailsMiddleware                                    │
│     Passthrough for MVP (future: content filtering, PII)    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  4. AccumulatorMiddleware                                   │
│     Yields chunks as passthrough while also accumulating    │
│     text, thinking, tool calls into a new AssistantMessage; │
│     appends to context.Messages on stream end               │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  5. ProviderMiddleware                                      │
│     Calls AI provider StreamCompletionAsync()               │
│     Yields ModelMiddlewareMessage chunks                    │
└─────────────────────────────────────────────────────────────┘

RESPONSE FLOW (up - IAsyncEnumerable streaming):
┌─────────────────────────────────────────────────────────────┐
│  Client receives stream of:                                 │
│  - ModelMiddlewareMessage (content (mime type))             │
│  - ModelMiddlewareToolRequestMessage (tool being called)    │
│  - ModelMiddlewareToolResponseMessage (tool result)         │
│  - GuardrailsMiddlewareMessage (policy actions)             │
└─────────────────────────────────────────────────────────────┘
```

---

## Failure Handling

### 1. Exception Wrapping (BaseExceptionMiddleware)

```csharp
// Middleware/BaseExceptionMiddleware.cs
public IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
    ModelMiddlewareContext context,
    Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next)
{
    var safeNext = next.WithExceptionHandler(this, this.HandleException);
    return safeNext(context);
}

protected virtual BaseMiddlewareMessage HandleException(Exception exception, ModelMiddlewareContext context)
{
    this.logger.LogError(exception, "An error occurred while processing the middleware request.");
    var errorMessage = BuildDetailedErrorMessage(exception);

    return new ModelMiddlewareMessage()
    {
        ModelMessage = new ModelResponseErrorContent()
        {
            ErrorMessage = errorMessage,
            Exception = exception,
        },
    };
}
```

### 2. Exception Handler Extension

```csharp
// ExceptionHandlingExtensions.cs
private static async IAsyncEnumerable<BaseMiddlewareMessage> WrapWithHandlerAsync(
    Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next,
    ModelMiddlewareContext context,
    Func<Exception, ModelMiddlewareContext, BaseMiddlewareMessage> onException,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await using var enumerator = next(context).GetAsyncEnumerator(cancellationToken);
    while (true)
    {
        BaseMiddlewareMessage? result;
        bool hasResult;

        try
        {
            hasResult = await enumerator.MoveNextAsync().ConfigureAwait(false);
            result = hasResult ? enumerator.Current : null;
        }
        catch (Exception ex)
        {
            result = onException(ex, context);  // Convert exception to message
            hasResult = true;
        }

        if (!hasResult) break;
        if (result == null) continue;
        yield return result;
    }
}
```

Key: Catches exceptions during enumeration and converts them to messages.

### 3. Tool Execution Failures

```csharp
// Middleware/ToolMiddleware.cs - HandleToolCallsAsync
catch (Exception ex)
{
    this.logger.LogError(ex, "Failed to execute tool {ToolName}.", tool.ToolName);
    var mcpFormatErrorResponse = ToolsResponseMapper.CreateExceptionResponse(ex.Message);
    responseMessage.Response = ToolsResponseMapper.ToJson(mcpFormatErrorResponse);
    responseMessage.Success = false;  // Marked as failed
    // ... yields the failed response message
}
```

---

## Middleware Implementations

### ProviderMiddleware (Streaming Source)

```csharp
// Middleware/ProviderMiddleware.cs
public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
    ModelMiddlewareContext context,
    Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // ... setup code ...

    var aiClient = await this.aiApiClientFactory.CreateClientAsync(clientConfig, cancellationToken);

    await foreach (var message in aiClient.StreamCompletionAsync(
                 context.Messages,
                 ToolUtils.GetToolDefinitionsWithoutStaticParameters(...),
                 context.ProviderParameters)
                 .WithCancellation(cancellationToken))
    {
        yield return new ModelMiddlewareMessage
        {
            ModelMessage = message,
        };
    }
}
```

> **TODO:** Define `IAiClient` interface with `StreamCompletionAsync` method. This abstraction will support multiple providers (OpenAI, Anthropic, etc.)

**Provider Responsibility:** The provider client handles all provider-specific streaming logic internally:
- Accumulates partial tool call arguments (Gemini `partialArgs`) into complete `ModelResponseToolCall` before yielding
- Normalizes provider-specific event types into `ModelResponseBase` subtypes
- Accumulates non-standard metadata for `ModelResponseStreamEnd`

The middleware layer receives a clean, normalized stream regardless of provider quirks.

### GuardrailsMiddleware (Passthrough)

```csharp
// Middleware/GuardrailsMiddleware.cs
public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
    ModelMiddlewareContext context,
    Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    // TODO: Implement guardrails logic (content filtering, PII detection, etc.)
    await foreach (var message in next(context).WithCancellation(cancellationToken))
    {
        yield return message;
    }
}
```

### ToolMiddleware (Looping Pattern)

```csharp
// Middleware/ToolMiddleware.cs
public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
    ModelMiddlewareContext context,
    Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var maxIterations = context.Variables.TryGetValue(MiddlewareVariables.MaxToolExecutions, out var val)
        ? (int)val
        : 20;  // Default

    var currentIteration = 0;
    bool hasToolCalls;

    do
    {
        List<ModelResponseBase> modelMessages = [];

        // Consume all messages from next middleware
        await foreach (var message in next(context).WithCancellation(cancellationToken))
        {
            if (message is ModelMiddlewareMessage modelMessage)
            {
                modelMessages.Add(modelMessage.ModelMessage);

                // Swallow tool calls (don't yield yet)
                if (modelMessage.ModelMessage is ModelResponseToolCall)
                    continue;
            }
            yield return message;  // Yield non-tool-call messages
        }

        hasToolCalls = modelMessages.OfType<ModelResponseToolCall>().Any();
        if (hasToolCalls)
        {
            // Execute tools and yield request/response messages
            await foreach (var message in this.HandleToolCallsAsync(context, modelMessages)
                .WithCancellation(cancellationToken))
            {
                yield return message;
            }
        }

        if (++currentIteration >= maxIterations)
        {
            this.logger.LogWarning("Max iterations ({MaxIterations}) reached for tool calls.", maxIterations);
            yield break;
        }
    }
    while (hasToolCalls);  // Loop if more tool calls
}
```

### Safety Net: Empty Terminal Delegate

In `ModelPipeline.cs`, the recursive construction handles the boundary case:

```csharp
private Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> CreatePipelineFunc(
    List<Type> middlewareTypes,
    int index)
{
    // Base case: past the end of the chain
    if (index >= middlewareTypes.Count)
    {
        return _ => EmptyAsyncEnumerableAsync<BaseMiddlewareMessage>();
    }
    // ...
}

private static async IAsyncEnumerable<T> EmptyAsyncEnumerableAsync<T>()
{
    yield break;  // Returns nothing
}
```

This empty delegate is passed to ProviderMiddleware as its `next` parameter, but since ProviderMiddleware ignores it, it's just a safety net that would return an empty stream if called.

---

## Post-MVP Considerations

### Multimodal Output (Image/Audio)
- `ModelResponseImageContent` - base64 data + mime type (image/png, image/jpeg)
- `ModelResponseAudioContent` - base64 data + mime type (audio/mp3, audio/wav) + optional transcript
- Add `Image` and `Audio` to `ContentBlockType` enum
- AccumulatorMiddleware may need special handling for binary content

### Realtime/Live Audio
- OpenAI Realtime API and Gemini Live API use WebSocket bidirectional streaming
- Different architecture from request/response IAsyncEnumerable pattern
- Will be handled by separate **RealtimeMiddleware** pipeline

### Citations
- `ModelResponseCitationContent` with location info (page, char, content block)
- OpenAI: `url_citation` with URL + title
- Anthropic: location types (page_location, char_location, content_block_location)  