# Fix 1: Linger Agents Deliver Result Before Going Idle

## Problem

When an agent has `Lifecycle = Linger`, `ReportToRegistryAsync` in `AgentGrain` enters the idle branch and returns early — skipping `ReportCompletionAsync` and `DeliverToParentAsync` entirely. The agent does its work and produces output, but the result is never delivered to the conversation grain. The parent orchestrator never sees what the agent did.

```
Normal agent:  complete → deliver result to parent → deactivate
Linger agent:  complete → go idle → ❌ result never delivered
```

## Root Cause

`AgentGrain.ReportToRegistryAsync` (line ~459):

```csharp
if (!isError && contract.Lifecycle == AgentLifecycle.Linger)
{
    _isIdle = true;
    await registry.ReportIdleAsync(GrainContext.GrainKey);
    Emit(new StreamAgentIdleEvent(GrainContext.GrainKey));
    DelayDeactivation(...);
    return; // <-- early return, skips DeliverToParentAsync
}
```

## Fix

For Linger agents, deliver the result to the parent (via `DeliverToParentAsync` in the registry) **then** go idle. The agent should report idle to the registry, but the result must still flow back to the conversation grain so the orchestrator sees it.

The simpler option is to have `AgentGrain` call `DeliverToParentAsync`'s equivalent directly — get the conversation grain and call `DeliverAgentResultAsync` — then report idle to the registry separately.

## Design Note (from Claude Code research)

Claude Code's model validates this approach: their agents complete work, deliver the result, then go idle. Resumption is explicit via `SendMessage` which treats the message as a new turn appended to the preserved transcript. Our `ResumeFromIdleAsync` already follows the same pattern — drain inbox, run pipeline. The key insight is that **delivering the result and going idle are not mutually exclusive** — the result flows to the parent, the agent stays alive for follow-up.

## Files to Change

- `src/actors/DonkeyWork.Agents.Actors.Core/Grains/AgentGrain.cs` — `ReportToRegistryAsync`: deliver result before going idle

## Verification

1. Build: `dotnet build DonkeyWork.Agents.sln`
2. Unit tests: `dotnet test test/actors/DonkeyWork.Agents.Actors.Tests`
3. Manual test: spawn a Linger agent, verify the result appears in the conversation AND the agent shows as idle (not completed) in the registry
4. Verify the agent can still be resumed via `send_message` after going idle
