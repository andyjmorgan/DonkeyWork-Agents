# Orleans Module

Actor-based agent orchestration using Microsoft Orleans 10.

## Structure

```
DonkeyWork.Agents.Orleans.Contracts/   # Grain interfaces, protocol types, events, messages
DonkeyWork.Agents.Orleans.Core/        # Grain implementations, middleware, interceptors
DonkeyWork.Agents.Orleans.Api/         # DI registration, options, WebSocket endpoints
```

## Key Conventions

- Grain keys encode userId for tenant isolation: `conv:{userId}:{conversationId}`
- All serializable types use `[GenerateSerializer]` and `[Id(N)]` attributes
- One class per file (following project conventions)
- `IAgentResponseObserver` is the grain observer interface for streaming events to clients
- No RabbitMQ — grain observers handle real-time event delivery directly
- SeaweedFS for grain state persistence (custom `IGrainStorage` provider)

## Grain Types

- `IConversationGrain` — Long-lived conversation orchestrator with message queue
- `IAgentGrain` — Single-execution sub-agent worker
- `IAgentRegistryGrain` — Per-conversation tracker for spawned agents

## Registration

```csharp
// In Program.cs
builder.Host.AddOrleansApi(builder.Configuration);
builder.Services.AddOrleansServices(builder.Configuration);
```
