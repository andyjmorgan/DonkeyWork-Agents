# Milestones

Implementation milestones for the orchestration platform evolution.

## MVP Milestones

| # | Milestone | Status | Dependencies |
|---|-----------|--------|--------------|
| M1 | [Orchestration Rename & Interfaces](./m1-orchestration-rename.md) | 🔄 In Progress | — |
| M2 | [MCP Server — Native Tools](./m2-mcp-server-native-tools.md) | 🔄 In Progress | M1 |
| M3 | [Chat Interface](./m3-chat-interface.md) | 🔲 Planned | M1, M2 |
| M4 | [MCP OAuth](./m4-mcp-oauth.md) | 🔲 Post-MVP | M2 |
| M5 | [A2A Server](./m5-a2a-server.md) | 🔲 Planned | M1 |

## Post-MVP Milestones

| # | Milestone | Status | Dependencies |
|---|-----------|--------|--------------|
| M7 | [MCP Server — User Orchestrations](./m7-mcp-server-user-orchestrations.md) | 🔲 Post-MVP | M1, M2 |

## Dependency Graph

```
M1: Orchestration Rename
 │
 ├──► M2: MCP Native Tools
 │     │
 │     ├──► M3: Chat Interface
 │     │
 │     ├──► M4: MCP OAuth (post-MVP)
 │     │
 │     └──► M7: MCP User Orchestrations (post-MVP)
 │
 └──► M5: A2A Server
```

## Detailed Plans

- [M1 Detailed Rename Plan](./m1-orchestration-rename-plan.md) — File-by-file breakdown

## Status Legend

- 🔲 Planned
- 🔄 In Progress
- ✅ Complete
- ⏸️ On Hold
