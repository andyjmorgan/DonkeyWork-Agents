# Orleans Contracts

Shared grain interfaces, protocol types, events, and messages for the Orleans actor system.

## Folders

- `Grains/` — Grain interfaces (`IConversationGrain`, `IAgentGrain`, `IAgentRegistryGrain`, `IAgentResponseObserver`)
- `Contracts/` — Agent configuration (`AgentContract`, lifecycle, search/fetch configs)
- `Models/` — Data types (`AgentResult`, `TrackedAgent`, `AgentKeys`, `CancelScope`)
- `Messages/` — Internal message protocol (`InternalMessage` hierarchy, content blocks)
- `Events/` — Stream events for real-time UI updates (`StreamEventBase` hierarchy)

## Serialization

All types exchanged between grains use Orleans serialization:
- `[GenerateSerializer]` on classes/records
- `[Id(N)]` on serialized properties
- `[RegisterCopier]` for types needing custom deep copy (e.g., `JsonElement`)
