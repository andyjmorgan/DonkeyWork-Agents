# M4: MCP OAuth (Post-MVP)

> **Status: Post-MVP** — Descoped from initial release.

## Overview

Implement MCP OAuth dance for secure authentication flow with MCP clients.

## Goals

1. Implement MCP OAuth authorization flow per spec
2. Token management (access, refresh)
3. Scope-based permissions for tools
4. Integration with existing Keycloak

## Deliverables

### OAuth Flow Implementation

- [ ] Authorization endpoint
- [ ] Token endpoint
- [ ] Token refresh endpoint
- [ ] Revocation endpoint

### Token Management

- [ ] Access token generation
- [ ] Refresh token generation
- [ ] Token storage (or stateless JWT)
- [ ] Token expiration handling

### Scope System

- [ ] Define scope taxonomy for tools
- [ ] Scope validation on tool calls
- [ ] Scope requirements in tool metadata (`RequiredScopes`)

### Keycloak Integration

- [ ] Leverage existing Keycloak for identity
- [ ] Map Keycloak tokens to MCP tokens (or passthrough)
- [ ] Handle Keycloak session management

## OAuth Flow

```
┌─────────────────┐                  ┌─────────────────┐
│   MCP Client    │                  │   MCP Server    │
└────────┬────────┘                  └────────┬────────┘
         │                                    │
         │  1. Authorization Request          │
         │  GET /oauth/authorize              │
         │  ?client_id=...&scope=...          │
         │ ─────────────────────────────────► │
         │                                    │
         │  2. Redirect to Keycloak           │
         │ ◄───────────────────────────────── │
         │                                    │
         │  3. User authenticates             │
         │ ─────────────────────────────────► │
         │                                    │
         │  4. Authorization Code             │
         │ ◄───────────────────────────────── │
         │                                    │
         │  5. Token Request                  │
         │  POST /oauth/token                 │
         │ ─────────────────────────────────► │
         │                                    │
         │  6. Access + Refresh Tokens        │
         │ ◄───────────────────────────────── │
         │                                    │
```

## Scope Taxonomy

```
orchestrations:read     - List and view orchestrations
orchestrations:execute  - Execute orchestrations
tasks:read              - List and view tasks
tasks:write             - Create, update, delete tasks
milestones:read         - List and view milestones
milestones:write        - Create, update, delete milestones
```

## Dependencies

- M2: MCP Server Native Tools (server infrastructure)
- M3: MCP Server User Orchestrations (tool execution)

## References

- [MCP OAuth Specification](https://spec.modelcontextprotocol.io/)
- OAuth 2.0 RFC 6749
