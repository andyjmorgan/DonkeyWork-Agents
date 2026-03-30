# Actors Core

Grain implementations, AI provider abstraction, and middleware pipeline for agent orchestration.

## Middleware Pipeline

The pipeline uses a right-to-left fold composition pattern. Each middleware wraps the next, forming a chain:

```
Exception -> Tool -> Guardrails -> Accumulator -> UsageTracking -> Provider
```

- **ExceptionMiddleware** (outermost) — catches exceptions from inner pipeline, yields `ErrorMessage`
- **ToolMiddleware** — agentic tool loop with eager parallel execution (max 25 iterations)
- **GuardrailsMiddleware** — pass-through stub for future content filtering
- **AccumulatorMiddleware** — accumulates streamed content blocks into `InternalAssistantMessage`
- **UsageTrackingMiddleware** — tracks token usage from provider responses
- **ProviderMiddleware** (terminal) — calls `IAiProviderFactory` to get a provider and stream completions

`ModelPipeline` builds the chain via `ActivatorUtilities.CreateInstance` and folds right-to-left so `ExceptionMiddleware.ExecuteAsync` is called first, with each middleware receiving a `next` delegate pointing to the middleware below it.

## Provider Abstraction

`IAiProvider` defines a single method `StreamCompletionAsync` that returns `IAsyncEnumerable<ModelResponseBase>`. The `IAiProviderFactory` creates provider instances by `ProviderType`.

### Adding a New Provider

1. Create a class implementing `IAiProvider` in `Providers/{ProviderName}/`
2. Add a value to the `ProviderType` enum
3. Add a case to `AiProviderFactory.Create()`

## Adding New Middleware

1. Create a class implementing `IModelMiddleware` in `Middleware/`
2. Add the type to `ModelPipeline.StandardPipeline` array at the desired position
3. The middleware receives a `ModelMiddlewareContext` and a `next` delegate
4. Call `next(context)` to invoke the inner pipeline, or skip it for terminal middleware

## Key Types

- `ModelMiddlewareContext` — carries messages, system prompt, tools, provider options, TurnId, PersistMessage callback, and cancellation through the pipeline
- `ResponsePartsBuilder` — accumulates text/thinking content during streaming
- `GrainContext` — scoped service populated by `GrainContextInterceptor` with grain key, userId, and conversationId
