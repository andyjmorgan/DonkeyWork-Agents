# M1: Orchestration Rename & Interfaces Schema

> Detailed implementation plan for renaming Agent → Orchestration and adding interfaces schema.

## Scope Summary

### Terminology Change

| Old | New |
|-----|-----|
| Agent | Orchestration |
| AgentVersion | OrchestrationVersion |
| AgentExecution | OrchestrationExecution |
| AgentNodeExecution | OrchestrationNodeExecution |
| AgentExecutionLog | OrchestrationExecutionLog |
| AgentVersionCredentialMapping | OrchestrationVersionCredentialMapping |
| AgentService | OrchestrationService |
| AgentVersionService | OrchestrationVersionService |
| AgentOrchestrator | OrchestrationExecutor |
| AgentExecutionRepository | OrchestrationExecutionRepository |

### New: Interfaces Schema

Add to `OrchestrationVersion`:
```csharp
public OrchestrationInterfaces Interfaces { get; set; } = new();
```

Add to execution context:
```csharp
public enum ExecutionInterface { Direct, MCP, A2A, Chat, Webhook }
```

---

## Files to Rename/Modify

### 1. Backend - Persistence Layer

#### Entities (rename files + class names)
```
src/common/DonkeyWork.Agents.Persistence/Entities/Agents/
├── AgentEntity.cs                          → Orchestrations/OrchestrationEntity.cs
├── AgentVersionEntity.cs                   → Orchestrations/OrchestrationVersionEntity.cs
├── AgentExecutionEntity.cs                 → Orchestrations/OrchestrationExecutionEntity.cs
├── AgentNodeExecutionEntity.cs             → Orchestrations/OrchestrationNodeExecutionEntity.cs
├── AgentExecutionLogEntity.cs              → Orchestrations/OrchestrationExecutionLogEntity.cs
└── AgentVersionCredentialMappingEntity.cs  → Orchestrations/OrchestrationVersionCredentialMappingEntity.cs
```

#### Configurations (rename files + class names)
```
src/common/DonkeyWork.Agents.Persistence/Configurations/Agents/
├── AgentConfiguration.cs                          → Orchestrations/OrchestrationConfiguration.cs
├── AgentVersionConfiguration.cs                   → Orchestrations/OrchestrationVersionConfiguration.cs
├── AgentExecutionConfiguration.cs                 → Orchestrations/OrchestrationExecutionConfiguration.cs
├── AgentNodeExecutionConfiguration.cs             → Orchestrations/OrchestrationNodeExecutionConfiguration.cs
├── AgentExecutionLogConfiguration.cs              → Orchestrations/OrchestrationExecutionLogConfiguration.cs
└── AgentVersionCredentialMappingConfiguration.cs  → Orchestrations/OrchestrationVersionCredentialMappingConfiguration.cs
```

#### Repositories (rename folder)
```
src/common/DonkeyWork.Agents.Persistence/Repositories/Agents/
└── (any files) → Orchestrations/
```

#### DbContext
```
src/common/DonkeyWork.Agents.Persistence/AgentsDbContext.cs
- Rename DbSet properties:
  - Agents → Orchestrations
  - AgentVersions → OrchestrationVersions
  - AgentExecutions → OrchestrationExecutions
  - AgentNodeExecutions → OrchestrationNodeExecutions
  - AgentExecutionLogs → OrchestrationExecutionLogs
  - AgentVersionCredentialMappings → OrchestrationVersionCredentialMappings
```

### 2. Backend - Contracts Layer

#### Project Rename
```
src/agents/DonkeyWork.Agents.Agents.Contracts/
→ src/orchestrations/DonkeyWork.Agents.Orchestrations.Contracts/
```

#### Services (rename interfaces)
```
Services/
├── IAgentService.cs           → IOrchestrationService.cs
├── IAgentVersionService.cs    → IOrchestrationVersionService.cs
├── IAgentOrchestrator.cs      → IOrchestrationExecutor.cs
├── IAgentExecutionRepository.cs → IOrchestrationExecutionRepository.cs
└── (others remain unchanged - IExecutionContext, ITemplateRenderer, etc.)
```

#### Models (rename files + class names)
```
Models/
├── CreateAgentRequestV1.cs    → CreateOrchestrationRequestV1.cs
├── CreateAgentResponseV1.cs   → CreateOrchestrationResponseV1.cs
├── GetAgentResponseV1.cs      → GetOrchestrationResponseV1.cs
├── GetAgentVersionResponseV1.cs → GetOrchestrationVersionResponseV1.cs
├── ExecuteAgentRequestV1.cs   → ExecuteOrchestrationRequestV1.cs
├── ExecuteAgentResponseV1.cs  → ExecuteOrchestrationResponseV1.cs
└── (others remain unchanged)
```

### 3. Backend - Core Layer

#### Project Rename
```
src/agents/DonkeyWork.Agents.Agents.Core/
→ src/orchestrations/DonkeyWork.Agents.Orchestrations.Core/
```

#### Services (rename files + class names)
```
Services/
├── AgentService.cs            → OrchestrationService.cs
├── AgentVersionService.cs     → OrchestrationVersionService.cs
├── AgentOrchestrator.cs       → OrchestrationExecutor.cs
├── AgentExecutionRepository.cs → OrchestrationExecutionRepository.cs
└── (others remain unchanged)
```

#### Options
```
Options/
└── AgentsOptions.cs           → OrchestrationsOptions.cs
```

### 4. Backend - API Layer

#### Project Rename
```
src/agents/DonkeyWork.Agents.Agents.Api/
→ src/orchestrations/DonkeyWork.Agents.Orchestrations.Api/
```

#### Controllers (rename files + class names + routes)
```
Controllers/
├── AgentsController.cs        → OrchestrationsController.cs
│   Route: /api/v1/agents      → /api/v1/orchestrations
├── AgentVersionsController.cs → OrchestrationVersionsController.cs
│   Route: /api/v1/agents/{id}/versions → /api/v1/orchestrations/{id}/versions
└── ExecutionsController.cs    (keep name, update route)
    Route: /api/v1/agents/{agentId}/execute → /api/v1/orchestrations/{orchestrationId}/execute
    Route: /api/v1/agents/executions → /api/v1/orchestrations/executions
```

#### DependencyInjection
```
DependencyInjection.cs
- AddAgentsApi() → AddOrchestrationsApi()
```

### 5. Backend - Test Projects

#### Project Renames
```
test/agents/DonkeyWork.Agents.Agents.Core.Tests/
→ test/orchestrations/DonkeyWork.Agents.Orchestrations.Core.Tests/
```

#### Integration Tests
```
test/integration/DonkeyWork.Agents.Integration.Tests/Tests/Controllers/
├── AgentsControllerTests.cs   → OrchestrationsControllerTests.cs
├── AgentVersionsControllerTests.cs → OrchestrationVersionsControllerTests.cs
└── (update all test methods, URLs, etc.)
```

### 6. Frontend

#### Pages (rename files)
```
src/pages/
├── AgentsPage.tsx             → OrchestrationsPage.tsx
├── AgentEditorPage.tsx        → OrchestrationEditorPage.tsx
└── index.ts                   (update exports)
```

#### Components (rename files where "Agent" is in name)
```
src/components/editor/
└── AgentMetadataDialog.tsx    → OrchestrationMetadataDialog.tsx
```

#### API Client
```
src/lib/api.ts
- Update all endpoint URLs: /agents → /orchestrations
- Update type names
```

#### Store
```
src/store/editor.ts
- Rename agent-related state
```

#### Routes
```
src/App.tsx
- /agents → /orchestrations
- /agents/:id → /orchestrations/:id
```

#### Sidebar
```
src/components/layout/Sidebar.tsx
- Update navigation labels and links
```

### 7. Database Migration

Create migration: `RenameAgentsToOrchestrations`

```sql
-- Rename tables
ALTER TABLE agents.agents RENAME TO orchestrations;
ALTER TABLE agents.agent_versions RENAME TO orchestration_versions;
ALTER TABLE agents.agent_executions RENAME TO orchestration_executions;
ALTER TABLE agents.agent_node_executions RENAME TO orchestration_node_executions;
ALTER TABLE agents.agent_execution_logs RENAME TO orchestration_execution_logs;
ALTER TABLE agents.agent_version_credential_mappings RENAME TO orchestration_version_credential_mappings;

-- Rename schema
ALTER SCHEMA agents RENAME TO orchestrations;

-- Rename foreign key columns
ALTER TABLE orchestrations.orchestration_versions RENAME COLUMN agent_id TO orchestration_id;
ALTER TABLE orchestrations.orchestration_executions RENAME COLUMN agent_version_id TO orchestration_version_id;
-- (continue for all FK columns)

-- Add interfaces column to orchestration_versions
ALTER TABLE orchestrations.orchestration_versions
ADD COLUMN interfaces jsonb NOT NULL DEFAULT '{}';
```

### 8. Solution File Updates

```
DonkeyWork.Agents.sln
- Update project paths and names
```

### 9. Documentation

```
CLAUDE.md
- Update any references to agents module
docs/
- Update orchestration-vision.md if needed
```

---

## New Types to Add

### ExecutionInterface Enum

```csharp
// Location: DonkeyWork.Agents.Orchestrations.Contracts/Enums/ExecutionInterface.cs

namespace DonkeyWork.Agents.Orchestrations.Contracts.Enums;

/// <summary>
/// Specifies which interface triggered the orchestration execution.
/// </summary>
public enum ExecutionInterface
{
    /// <summary>
    /// Direct API call to /execute endpoint.
    /// </summary>
    Direct = 0,

    /// <summary>
    /// Invoked via MCP server as a tool.
    /// </summary>
    MCP = 1,

    /// <summary>
    /// Invoked via A2A protocol.
    /// </summary>
    A2A = 2,

    /// <summary>
    /// Invoked via chat interface.
    /// </summary>
    Chat = 3,

    /// <summary>
    /// Invoked via webhook (post-MVP).
    /// </summary>
    Webhook = 4
}
```

### OrchestrationInterfaces Model

```csharp
// Location: DonkeyWork.Agents.Orchestrations.Contracts/Models/OrchestrationInterfaces.cs

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Configuration for all interfaces an orchestration can be exposed through.
/// </summary>
public class OrchestrationInterfaces
{
    /// <summary>
    /// MCP tool interface configuration.
    /// </summary>
    public InterfaceConfig? MCP { get; set; }

    /// <summary>
    /// A2A agent interface configuration.
    /// </summary>
    public InterfaceConfig? A2A { get; set; }

    /// <summary>
    /// Chat interface configuration.
    /// </summary>
    public InterfaceConfig? Chat { get; set; }

    /// <summary>
    /// Webhook interface configuration (post-MVP).
    /// </summary>
    public InterfaceConfig? Webhook { get; set; }
}

/// <summary>
/// Configuration for a single interface.
/// </summary>
public class InterfaceConfig
{
    /// <summary>
    /// Whether this interface is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Name for this interface (e.g., MCP tool name, A2A agent name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description for this interface.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
```

### Update ExecutionContext

```csharp
// Add to IExecutionContext and ExecutionContext

/// <summary>
/// The interface that triggered this execution.
/// </summary>
ExecutionInterface Interface { get; }
```

### Update OrchestrationExecutionEntity

```csharp
// Add to entity

/// <summary>
/// The interface that triggered this execution.
/// </summary>
public ExecutionInterface Interface { get; set; } = ExecutionInterface.Direct;
```

---

## Execution Order

### Phase 1: Backend Rename (No Breaking Changes Yet)

1. Create new migration for table/column renames
2. Rename Persistence layer (entities, configurations, repositories)
3. Update DbContext DbSet properties
4. Rename Contracts layer (interfaces, models)
5. Rename Core layer (services)
6. Rename API layer (controllers, routes, DI)
7. Update solution file
8. Run all tests, fix any issues

### Phase 2: Add Interfaces Schema

1. Add `ExecutionInterface` enum
2. Add `OrchestrationInterfaces` model
3. Add `InterfaceConfig` model
4. Update `OrchestrationVersionEntity` with `Interfaces` property
5. Update `OrchestrationExecutionEntity` with `Interface` property
6. Update `IExecutionContext` and `ExecutionContext`
7. Create migration for new columns
8. Update API models (version request/response)
9. Update services to handle interfaces

### Phase 3: Frontend Rename

1. Update API client (URLs, types)
2. Rename pages
3. Rename components
4. Update routes
5. Update sidebar
6. Update store
7. Run frontend tests

### Phase 4: Test & Documentation

1. Rename test projects
2. Update all test files
3. Run full test suite
4. Update CLAUDE.md
5. Update any other documentation

---

## API Route Changes

| Old Route | New Route |
|-----------|-----------|
| `GET /api/v1/agents` | `GET /api/v1/orchestrations` |
| `POST /api/v1/agents` | `POST /api/v1/orchestrations` |
| `GET /api/v1/agents/{id}` | `GET /api/v1/orchestrations/{id}` |
| `PUT /api/v1/agents/{id}` | `PUT /api/v1/orchestrations/{id}` |
| `DELETE /api/v1/agents/{id}` | `DELETE /api/v1/orchestrations/{id}` |
| `GET /api/v1/agents/{id}/versions` | `GET /api/v1/orchestrations/{id}/versions` |
| `GET /api/v1/agents/{id}/versions/{versionId}` | `GET /api/v1/orchestrations/{id}/versions/{versionId}` |
| `POST /api/v1/agents/{id}/versions` | `POST /api/v1/orchestrations/{id}/versions` |
| `POST /api/v1/agents/{id}/publish` | `POST /api/v1/orchestrations/{id}/publish` |
| `POST /api/v1/agents/{agentId}/execute` | `POST /api/v1/orchestrations/{orchestrationId}/execute` |
| `POST /api/v1/agents/{agentId}/test` | `POST /api/v1/orchestrations/{orchestrationId}/test` |
| `GET /api/v1/agents/executions` | `GET /api/v1/orchestrations/executions` |
| `GET /api/v1/agents/executions/{id}` | `GET /api/v1/orchestrations/executions/{id}` |
| `GET /api/v1/agents/executions/{id}/stream` | `GET /api/v1/orchestrations/executions/{id}/stream` |
| `GET /api/v1/agents/executions/{id}/logs` | `GET /api/v1/orchestrations/executions/{id}/logs` |
| `GET /api/v1/agents/executions/{id}/nodes` | `GET /api/v1/orchestrations/executions/{id}/nodes` |

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Breaking API changes | Version the API; old routes could redirect (not recommended for MVP) |
| Database migration failures | Test migration on copy of production data |
| Missed renames | Use IDE refactoring tools; grep for remaining "Agent" references |
| Frontend/backend mismatch | Deploy backend first, then frontend in same release |

---

## Checklist

- [ ] Create database migration
- [ ] Rename Persistence entities
- [ ] Rename Persistence configurations
- [ ] Update DbContext
- [ ] Rename Contracts interfaces
- [ ] Rename Contracts models
- [ ] Rename Core services
- [ ] Rename API controllers
- [ ] Update API routes
- [ ] Update DependencyInjection
- [ ] Update solution file
- [ ] Add ExecutionInterface enum
- [ ] Add OrchestrationInterfaces model
- [ ] Add InterfaceConfig model
- [ ] Update version entity with Interfaces
- [ ] Update execution entity with Interface
- [ ] Update ExecutionContext with Interface
- [ ] Create migration for new columns
- [ ] Rename frontend pages
- [ ] Rename frontend components
- [ ] Update frontend API client
- [ ] Update frontend routes
- [ ] Update frontend store
- [ ] Rename test projects
- [ ] Update test files
- [ ] Run full test suite
- [ ] Update CLAUDE.md
- [ ] Update documentation
