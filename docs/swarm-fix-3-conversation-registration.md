# Fix 3: Register Conversation Grain in Registry for Bidirectional Messaging

## Problem

Spawned agents cannot message the parent orchestrator (e.g. "navi") because the conversation grain is not registered in the agent registry. When an agent tries `send_message(target="navi")`, the registry returns "Agent 'navi' not found".

## Current Architecture

- `AgentRegistryGrain` only tracks spawned `AgentGrain` instances
- `SendMessageAsync` calls `GrainFactory.GetGrain<IAgentGrain>(toAgentKey)` — this fails for conversation grain keys (`conv:...`)
- `IConversationGrain` has no `DeliverMessageAsync` method
- The conversation grain has a `_queue` (Channel) for processing messages, and already drains it during pipeline execution via `drainPendingMessages`

## Fix

### 1. Add message delivery to IConversationGrain

Add `DeliverMessageAsync(AgentMessage message)` to `IConversationGrain`. Mark it `[AlwaysInterleave]` so it can be called while the grain is processing a turn.

### 2. Implement in ConversationGrain

Write the incoming `AgentMessage` to the `_queue` as a new message type (e.g. `AgentMessageConversationMessage`) so it flows through the existing processing loop. The `FormatMessage` method formats it into an `InternalContentMessage` with the `<agent-message>` XML tag, same as `AgentGrain.DrainInboxMessages`.

### 3. Register conversation grain in registry

During the first turn in `ProcessQueueAsync`, register the conversation grain with the registry:
```csharp
var registryKey = AgentKeys.Conversation(userId, conversationId);
var registry = GrainFactory.GetGrain<IAgentRegistryGrain>(registryKey);
await registry.RegisterAsync(grainKey, "Orchestrator", displayName ?? "navi", grainKey);
```

The conversation grain registers itself as its own parent (it's the root).

### 4. Route conv: keys in SendMessageAsync

In `AgentRegistryGrain.SendMessageAsync`, check the key prefix. If the target key starts with `conv:`, get `IConversationGrain` instead of `IAgentGrain`:

```csharp
if (toAgentKey.StartsWith(AgentKeys.ConversationPrefix))
{
    var convGrain = GrainFactory.GetGrain<IConversationGrain>(toAgentKey);
    await convGrain.DeliverMessageAsync(message);
}
else
{
    var grain = GrainFactory.GetGrain<IAgentGrain>(toAgentKey);
    await grain.DeliverMessageAsync(message);
}
```

### 5. Set MessageInbox on ConversationGrain's GrainContext

The conversation grain also needs `GrainContext.MessageInbox` set so that `check_messages` works for the orchestrator. Create a `Channel<AgentMessage>` field and assign it, same as `AgentGrain`.

Alternatively, since the conversation grain already has the `_queue` drain mechanism, the `check_messages` tool can just read from `MessageInbox` and the `_queue` drain handles the rest.

## Design Note (from Claude Code research)

Claude Code uses a similar pattern: in-process teammates queue messages in `pendingUserMessages[]` which are delivered at the next tool round. Our `_queue` + `drainPendingMessages` mechanism is the Orleans equivalent. The key principle from Claude Code is **"your output is not visible to teammates"** — agents must explicitly use `send_message` to communicate; completing a task only delivers to the parent, not siblings. This is already how our architecture works and this fix ensures the parent is actually reachable.

## Files to Change

- `src/actors/DonkeyWork.Agents.Actors.Contracts/Grains/IConversationGrain.cs` — add `DeliverMessageAsync`
- `src/actors/DonkeyWork.Agents.Actors.Core/Grains/ConversationGrain.cs` — implement delivery, register in registry, add new message type
- `src/actors/DonkeyWork.Agents.Actors.Core/Grains/AgentRegistryGrain.cs` — route `conv:` keys in `SendMessageAsync`

## Verification

1. Build: `dotnet build DonkeyWork.Agents.sln`
2. Unit tests: `dotnet test test/actors/DonkeyWork.Agents.Actors.Tests`
3. Full test suite: `dotnet test DonkeyWork.Agents.sln`
4. Manual test: spawn a Linger agent, let it complete, then from the agent use `send_message(target="navi", message="here are my findings")` — the message should appear in the conversation
5. Verify `list_agents` from the conversation grain shows the conversation grain itself (for roster purposes)
6. Verify the conversation grain is NOT removed from the registry (it should never be collected/tombstoned)
