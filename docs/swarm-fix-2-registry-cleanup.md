# Fix 2: Deferred Cleanup of Terminal Agents from Registry

## Problem

When `wait_for_agent` collects an agent's result, the agent entry remains in the registry with `Delivered = true` and `Status = Completed`. These dead entries clutter the registry and will appear in the swarm roster (Fix 4), confusing the orchestrator with agents it can no longer interact with.

Similarly, when a Linger agent's `DelayDeactivation` timer expires, Orleans deactivates the grain silently. The registry entry stays as `Idle` forever — a ghost pointing to a deactivated grain with no state. If someone messages it, Orleans reactivates the grain fresh but all in-memory state (contract, messages, inbox) is gone, causing errors.

## Fix

### Deferred removal with 5-minute grace period

Instead of removing entries immediately after collection, mark them with a `CollectedAt` timestamp. A periodic cleanup sweep (Orleans grain timer) removes entries older than 5 minutes. This gives the orchestrator a window to still see the agent in the roster before it disappears, and avoids race conditions where the result is collected but the orchestrator is mid-turn referencing the name.

### Grain deactivation reports expiry

When a Linger agent's grain deactivates (linger timer expired), `OnDeactivateAsync` should report an `Expired` status to the registry. The expired entry gets the same `CollectedAt` timestamp and is cleaned up by the same sweep.

### What gets cleaned up

Entries are eligible for cleanup when:
- Status is terminal (`Completed`, `Failed`, `Cancelled`, `Expired`) AND `CollectedAt` is set AND older than 5 minutes

Entries that are NOT cleaned up:
- `Idle` agents — still alive and can receive messages
- `Pending` agents — still running
- Terminal agents within the 5-minute grace window

### Implementation

1. Add `CollectedAt` (DateTimeOffset?) to `AgentEntry`
2. After `WaitForNextAsync` / `WaitForSpecificAsync` delivers a result, set `CollectedAt = DateTimeOffset.UtcNow` on the entry
3. Add `Expired` to `AgentStatus` enum
4. In `AgentGrain.OnDeactivateAsync`, if `_isIdle`, report `Expired` to registry
5. `ReportExpiredAsync` on registry sets status to `Expired` and `CollectedAt = DateTimeOffset.UtcNow`
6. Register an Orleans grain timer on `AgentRegistryGrain` (e.g. every 60 seconds) that removes entries where `CollectedAt` is older than 5 minutes, cleaning up both `_agents` and `_nameIndex`

## Files to Change

- `src/actors/DonkeyWork.Agents.Actors.Contracts/Models/AgentStatus.cs` — add `Expired`
- `src/actors/DonkeyWork.Agents.Actors.Contracts/Grains/IAgentRegistryGrain.cs` — add `ReportExpiredAsync`
- `src/actors/DonkeyWork.Agents.Actors.Core/Grains/AgentRegistryGrain.cs` — add `CollectedAt` to `AgentEntry`, implement deferred cleanup timer, implement `ReportExpiredAsync`
- `src/actors/DonkeyWork.Agents.Actors.Core/Grains/AgentGrain.cs` — report `Expired` in `OnDeactivateAsync` if idle

## Verification

1. Build: `dotnet build DonkeyWork.Agents.sln`
2. Unit tests: `dotnet test test/actors/DonkeyWork.Agents.Actors.Tests`
3. Add new tests:
   - `WaitForNextAsync` delivers result, entry still visible in `ListAsync` immediately after
   - After 5+ minutes (use short interval in test), cleanup sweep removes the entry
   - `ReportExpiredAsync` sets status and `CollectedAt`
   - Idle agents are NOT cleaned up
   - `ResolveAgentKeyByNameAsync` returns null after cleanup
4. Full test suite: `dotnet test DonkeyWork.Agents.sln`
