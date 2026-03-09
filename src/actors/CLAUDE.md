# Actors Module

Actor-based agent orchestration using Microsoft Orleans 10.

## Structure

```
DonkeyWork.Agents.Actors.Contracts/   # Grain interfaces, protocol types, events, messages
DonkeyWork.Agents.Actors.Core/        # Grain implementations, middleware, interceptors
DonkeyWork.Agents.Actors.Api/         # DI registration, options, WebSocket endpoints
```

## Key Conventions

- Grain keys encode userId for tenant isolation: `conv:{userId}:{conversationId}`
- All serializable types use `[GenerateSerializer]` and `[Id(N)]` attributes
- One class per file (following project conventions)
- `IAgentResponseObserver` is the grain observer interface for streaming events to clients
- NATS JetStream streaming provider available via `Orleans.Streaming.Nats` for grain-to-grain pub/sub
- Grain observers (`IAgentResponseObserver`) handle real-time event delivery to clients directly
- EF Core + PostgreSQL for grain message persistence via `IGrainMessageStore`

## Grain Types

- `IConversationGrain` — Long-lived conversation orchestrator with message queue
- `IAgentGrain` — Single-execution sub-agent worker
- `IAgentRegistryGrain` — Per-conversation tracker for spawned agents

## Registration

```csharp
// In Program.cs
builder.Host.AddActorsApi(builder.Configuration);
builder.Services.AddActorsServices(builder.Configuration);
```
