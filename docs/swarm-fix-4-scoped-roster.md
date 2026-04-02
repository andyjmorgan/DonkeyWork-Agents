# Fix 4: Scoped Roster Injection and Prompt Updates

## Problem

Agents have no visibility into the swarm topology. The orchestrator doesn't know which agents are idle and reusable. Sub-agents can see the entire flat agent list, enabling cross-branch messaging that breaks the chain of command.

## Fix

### 1. Build scoped call-tree roster

At turn start, build a roster from the registry showing only agents within the current agent's visibility boundary:
- **Parent** (one level up)
- **Self** (marked with "(you)")
- **Children and descendants** (the agent's subtree)

Agents outside this boundary are excluded. The roster is rendered as an indented call tree:

```
## Swarm
- navi (parent, active)
  - deep-researcher (you, active) — "Research OAuth patterns"
    - research_1 (running) — "Token refresh flows"
    - research_2 (running) — "PKCE implementation"
```

### 2. Inject roster into system prompt or turn context

Add the roster to the system prompt at turn start. The registry's `ListAsync` returns `TrackedAgent` records with `ParentAgentKey`, `Name`, `Status`, and `Label` — enough to build the tree.

Add a method to `AgentRegistryGrain`:
```csharp
Task<string> GetScopedRosterAsync(string agentKey);
```

This builds the tree, filters to the agent's boundary, and returns the rendered markdown.

**Inject once at turn start, don't update mid-turn.** Claude Code validates this — they don't re-render the system prompt when new agents spawn. Staleness mid-turn is acceptable since `send_message` already returns errors with the current agent list when targeting an unknown name.

### 3. Scope list_agents to boundary

`SwarmAgentManagementTools.ListAgents` currently calls `registry.ListAsync()` which returns all agents. Change it to only return agents within the caller's visibility boundary (parent + subtree).

Add a method to `AgentRegistryGrain`:
```csharp
Task<IReadOnlyList<TrackedAgent>> ListScopedAsync(string agentKey);
```

### 4. Scope wait_for_agent to children only

`wait_for_agent` and `wait_for_specific_agent` should only work for the caller's direct children. An agent shouldn't be able to wait on a sibling or a cousin.

### 5. Prompt updates — keep it lean

Claude Code's key learning: **minimal prompting wins.** Their teammate addendum is tiny. We should follow suit.

The prompt fragment should be short and direct:

- The roster is shown above — no need to call `list_agents` to discover the swarm
- Before spawning, check if an idle agent in the roster matches the topic — `send_message` to reuse it instead
- Your output is only visible to your parent — siblings cannot see it unless you use `send_message` or `write_shared_context`
- To communicate cross-branch, message your parent and let it coordinate

Avoid over-explaining the tree structure, status meanings, or verification procedures. The LLM can read the roster format. Don't add paragraphs of instructions — a few bullet points is enough.

### 6. Conversation grain excluded from cleanup

The conversation grain registers itself as root (Fix 3). It must be excluded from the deferred cleanup sweep (Fix 2) — it's never terminal. Filter by checking if the agent key starts with `conv:` or by never setting `CollectedAt` on it.

## Files to Change

- `src/actors/DonkeyWork.Agents.Actors.Contracts/Grains/IAgentRegistryGrain.cs` — add `GetScopedRosterAsync`, `ListScopedAsync`
- `src/actors/DonkeyWork.Agents.Actors.Core/Grains/AgentRegistryGrain.cs` — implement scoped listing and roster rendering
- `src/actors/DonkeyWork.Agents.Actors.Core/Grains/BaseAgentGrain.cs` — inject roster into system prompt at turn start
- `src/actors/DonkeyWork.Agents.Actors.Core/Tools/Swarm/SwarmAgentManagementTools.cs` — scope `list_agents` and `wait_for_agent`
- `src/actors/DonkeyWork.Agents.Actors.Core/Tools/Swarm/SwarmAgentMessagingTools.cs` — update prompt fragment (keep it lean)

## Verification

1. Build: `dotnet build DonkeyWork.Agents.sln`
2. Unit tests: `dotnet test test/actors/DonkeyWork.Agents.Actors.Tests`
3. Add new tests:
   - `GetScopedRosterAsync` returns only parent + subtree for a given agent
   - `ListScopedAsync` excludes agents outside boundary
   - Root agent (conversation grain) sees all its direct children but not grandchildren's siblings
   - Leaf agent sees only its parent and itself
4. Full test suite: `dotnet test DonkeyWork.Agents.sln`
5. Manual test:
   - Spawn two independent agents from the orchestrator
   - From one agent, verify `list_agents` does NOT show the other agent
   - Verify the roster in the system prompt shows correct tree with "(you)" marker
   - Ask the orchestrator to reuse an idle agent on the same topic — verify it sends a message instead of spawning
   - Ask the orchestrator for a different topic — verify it spawns a new agent
