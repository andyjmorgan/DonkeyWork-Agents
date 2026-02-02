# M1: Orchestration Rename & Interfaces Schema

## Overview

Rename Agent → Orchestration throughout the codebase and add the interfaces schema to support MCP, A2A, Chat, and Webhook exposure methods.

## Goals

1. Rename all Agent terminology to Orchestration
2. Add `OrchestrationInterfaces` model to version entity
3. Add `ExecutionInterface` enum to track invocation source
4. Update all API routes from `/agents` to `/orchestrations`

## Deliverables

### Backend

- [ ] Database migration for table/column renames
- [ ] Database migration for new `interfaces` and `execution_interface` columns
- [ ] Rename Persistence layer (entities, configurations, DbContext)
- [ ] Rename Contracts layer (interfaces, models)
- [ ] Rename Core layer (services)
- [ ] Rename API layer (controllers, routes)
- [ ] Add `ExecutionInterface` enum
- [ ] Add `OrchestrationInterfaces` model
- [ ] Add `InterfaceConfig` model
- [ ] Update `ExecutionContext` with `Interface` property

### Frontend

- [ ] Update API client URLs and types
- [ ] Rename pages (AgentsPage → OrchestrationsPage, etc.)
- [ ] Rename components (AgentMetadataDialog → OrchestrationMetadataDialog)
- [ ] Update routes
- [ ] Update sidebar navigation

### Tests

- [ ] Rename test projects
- [ ] Update all test files
- [ ] Pass full test suite

### Documentation

- [ ] Update CLAUDE.md
- [ ] Update any affected docs

## New Types

### ExecutionInterface Enum

```csharp
public enum ExecutionInterface
{
    Direct = 0,   // API call to /execute
    MCP = 1,      // Via MCP server
    A2A = 2,      // Via A2A protocol
    Chat = 3,     // Via chat interface
    Webhook = 4   // Via webhook (post-MVP)
}
```

### OrchestrationInterfaces Model

```csharp
public class OrchestrationInterfaces
{
    public InterfaceConfig? MCP { get; set; }
    public InterfaceConfig? A2A { get; set; }
    public InterfaceConfig? Chat { get; set; }
    public InterfaceConfig? Webhook { get; set; }
}

public class InterfaceConfig
{
    public bool Enabled { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

## API Route Changes

| Old | New |
|-----|-----|
| `/api/v1/agents` | `/api/v1/orchestrations` |
| `/api/v1/agents/{id}` | `/api/v1/orchestrations/{id}` |
| `/api/v1/agents/{id}/versions` | `/api/v1/orchestrations/{id}/versions` |
| `/api/v1/agents/{id}/execute` | `/api/v1/orchestrations/{id}/execute` |
| `/api/v1/agents/executions` | `/api/v1/orchestrations/executions` |

## Dependencies

- None (first milestone)

## Estimated Scope

- ~120 backend source files to rename/modify
- ~20 frontend files to update
- 2 database migrations
- Full test suite updates

## Risks

| Risk | Mitigation |
|------|------------|
| Breaking API changes | Single release with frontend + backend |
| Missed renames | Use IDE refactoring; grep verification |
| Migration failures | Test on copy of data first |

## Detailed Plan

See [m1-orchestration-rename-plan.md](./m1-orchestration-rename-plan.md) for comprehensive file-by-file breakdown.
