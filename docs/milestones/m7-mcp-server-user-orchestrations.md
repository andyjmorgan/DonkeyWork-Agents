# M7: MCP Server — User Orchestrations as Tools

## Overview

Expose user's MCP-enabled orchestrations as tools via the MCP server.

## Goals

1. Dynamically expose user orchestrations with MCP interface enabled
2. Generate tool schemas from orchestration InputSchema
3. Use interface-specific name/description
4. Execute orchestrations when tools are called

## Deliverables

### Dynamic Tool Discovery

- [ ] Query user's orchestrations with MCP interface enabled
- [ ] Register orchestrations as MCP tools at runtime
- [ ] Handle tool list refresh when orchestrations change
- [ ] Namespace user tools to avoid collisions

### Tool Schema Generation

- [ ] Map `InputSchema` (JSON Schema) → MCP tool input schema
- [ ] Map `OutputSchema` → MCP tool output schema (if defined)
- [ ] Use MCP interface `Name` as tool name
- [ ] Use MCP interface `Description` as tool description

### Tool Execution

- [ ] Route tool calls to orchestration executor
- [ ] Set `ExecutionInterface.MCP` in execution context
- [ ] Return orchestration output as tool result
- [ ] Handle streaming responses (if MCP supports)

### Tool Naming

- [ ] Format: `user/{orchestration-name}` or `{orchestration-name}`
- [ ] Validate name uniqueness per user
- [ ] Handle name conflicts gracefully

## Architecture

```
MCP Tool Call
      │
      ▼
┌─────────────────────────────────────┐
│  MCP Server                         │
│  ├── Resolve user from auth         │
│  ├── Find orchestration by tool name│
│  └── Validate MCP interface enabled │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│  Orchestration Executor             │
│  ├── ExecutionInterface = MCP       │
│  ├── Validate input against schema  │
│  └── Execute orchestration          │
└─────────────────┬───────────────────┘
                  │
                  ▼
┌─────────────────────────────────────┐
│  Return Result                      │
│  └── Map output to MCP response     │
└─────────────────────────────────────┘
```

## Interface Config Usage

```csharp
// From OrchestrationVersion.Interfaces.MCP
{
  "enabled": true,
  "name": "process-invoice",      // → MCP tool name
  "description": "Process an invoice and extract data"  // → MCP tool description
}
```

## Dependencies

- M1: Orchestration Rename (interfaces schema)
- M2: MCP Server Native Tools (infrastructure)

## Open Questions

- Cache orchestration tool list per user?
- Webhook for tool list invalidation?
- Support tool versioning (orchestration versions)?
