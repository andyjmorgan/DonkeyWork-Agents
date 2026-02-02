# M5: A2A Server

## Overview

Implement A2A (Agent-to-Agent) protocol server to expose orchestrations as callable A2A agents with agent cards and task lifecycle management.

## Goals

1. Generate agent cards from orchestration metadata
2. Implement A2A JSON-RPC endpoints
3. Track task lifecycle (working, completed, failed, etc.)
4. Map A2A message format to orchestration I/O

## Protocol Reference

| | |
|---|---|
| **Spec Version** | 0.3 (July 2025) |
| **Governance** | Linux Foundation |
| **GitHub** | https://github.com/a2aproject/A2A |
| **Specification** | https://a2a-protocol.org/latest/specification/ |

## Deliverables

### Agent Card Generation

- [ ] Generate agent card from orchestration metadata
- [ ] Use A2A interface `Name` and `Description`
- [ ] Include skills from orchestration capabilities
- [ ] Define security schemes (API key for MVP)
- [ ] Serve at `/.well-known/agent.json`

### A2A Endpoints

- [ ] `POST /a2a` — JSON-RPC 2.0 dispatcher
- [ ] `SendMessage` method handler
- [ ] `SendStreamingMessage` method handler (SSE)
- [ ] `GetTask` method handler
- [ ] `ListTasks` method handler
- [ ] `CancelTask` method handler

### Task Lifecycle Management

- [ ] Task state machine implementation
- [ ] States: working, completed, failed, canceled, rejected, input_required, auth_required
- [ ] Task persistence (or in-memory for MVP)
- [ ] Task-to-execution mapping

### Message Format Mapping

- [ ] Map A2A message parts → orchestration input
- [ ] Map orchestration output → A2A message parts
- [ ] Handle text, file, and structured data parts

### Authentication

- [ ] API key authentication (MVP)
- [ ] Define in agent card `securitySchemes`

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  A2A Client Agent                   │
└─────────────────────┬───────────────────────────────┘
                      │ A2A Protocol (JSON-RPC 2.0)
                      ▼
┌─────────────────────────────────────────────────────┐
│              DonkeyWork A2A Server                  │
├─────────────────────────────────────────────────────┤
│  /.well-known/agent.json  →  Agent Card            │
│  POST /a2a                →  JSON-RPC Dispatcher   │
├─────────────────────────────────────────────────────┤
│  Task Manager                                       │
│  ├── Create task from SendMessage                  │
│  ├── Track state transitions                       │
│  └── Map to OrchestrationExecution                 │
└─────────────────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────┐
│           Orchestration Execution Engine            │
│           ExecutionInterface = A2A                  │
└─────────────────────────────────────────────────────┘
```

## Agent Card Structure

```json
{
  "name": "invoice-processor",
  "description": "Processes invoices and extracts data",
  "provider": {
    "organization": "DonkeyWork"
  },
  "url": "https://a2a.agents.donkeywork.dev",
  "capabilities": {
    "streaming": true,
    "pushNotifications": false
  },
  "skills": [
    {
      "name": "process",
      "description": "Process an invoice",
      "inputSchema": { /* from orchestration InputSchema */ },
      "outputSchema": { /* from orchestration OutputSchema */ }
    }
  ],
  "securitySchemes": {
    "apiKey": {
      "type": "apiKey",
      "in": "header",
      "name": "X-API-Key"
    }
  },
  "defaultInputModes": ["application/json"],
  "defaultOutputModes": ["application/json"]
}
```

## Task State Machine

```
                    ┌─────────────┐
                    │   working   │◄──────────────────┐
                    └──────┬──────┘                   │
                           │                          │
       ┌───────────────────┼───────────────────┐      │
       ▼                   ▼                   ▼      │
┌─────────────┐    ┌─────────────┐    ┌─────────────┐ │
│  completed  │    │   failed    │    │  canceled   │ │
└─────────────┘    └─────────────┘    └─────────────┘ │
                                                      │
               ┌─────────────────┐                    │
               │ input_required  │────────────────────┘
               └─────────────────┘
                        ▲
                        │ (user provides input)
```

## A2A ↔ Orchestration Mapping

| A2A Concept | Orchestration Concept |
|-------------|----------------------|
| Agent | Orchestration (with A2A interface enabled) |
| Agent Card | Generated from orchestration metadata |
| Task | OrchestrationExecution |
| Task ID | ExecutionId |
| Message Parts | Execution Input/Output (JSON) |
| Task State | ExecutionStatus + custom states |

## MVP Scope

| Feature | MVP | Later |
|---------|-----|-------|
| Agent Card generation | ✓ | |
| SendMessage | ✓ | |
| SendStreamingMessage | ✓ | |
| GetTask | ✓ | |
| ListTasks | ✓ | |
| CancelTask | ✓ | |
| API key auth | ✓ | |
| Push notifications | | ✓ |
| gRPC transport | | ✓ |
| Call external A2A agents | | ✓ |

## Dependencies

- M1: Orchestration Rename (interfaces schema, ExecutionInterface)
- Existing orchestration execution engine

## References

- [A2A Protocol Specification](https://a2a-protocol.org/latest/specification/)
- [A2A GitHub Repository](https://github.com/a2aproject/A2A)
