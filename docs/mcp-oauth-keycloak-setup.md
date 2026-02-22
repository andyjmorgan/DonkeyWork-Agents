# MCP OAuth Keycloak Configuration

Documents the Keycloak Admin changes made to enable MCP OAuth discovery for ChatGPT and Claude integration.

**Realm:** `Agents`
**Keycloak URL:** `https://auth.donkeywork.dev`

## What Was Changed

### 1. Removed "Trusted Hosts" Client Registration Policy

**Why:** Keycloak's default "Trusted Hosts" policy restricts Dynamic Client Registration (DCR) to clients with redirect URIs matching trusted host patterns. MCP clients like ChatGPT (`https://chatgpt.com/*`) and Claude (`http://127.0.0.1:*`) use redirect URIs that don't match the default trusted hosts, causing DCR to fail.

**What was done:**
- Deleted the `Trusted Hosts` component (providerId: `trusted-hosts`) from the Agents realm's client registration policies

**API call:**
```
DELETE /admin/realms/Agents/components/{trusted-hosts-component-id}
```

**Remaining policies** (unchanged):
- Allowed Protocol Mapper Types (x2 - anonymous and authenticated)
- Max Clients Limit
- Allowed Client Scopes (x2 - anonymous and authenticated)
- Consent Required
- Full Scope Disabled

### 2. Created `mcp-audience` Client Scope

**Why:** The existing JWT audience validation checks for `donkeywork-agents-api` in the `aud` or `azp` claim. Tokens issued to DCR-registered clients (ChatGPT, Claude) won't have this audience by default, causing authentication to fail with a 401.

**What was done:**
- Created a new client scope named `mcp-audience`
- Protocol: `openid-connect`
- Not displayed on consent screen
- Not included as a token scope value

**API call:**
```json
POST /admin/realms/Agents/client-scopes
{
    "name": "mcp-audience",
    "description": "Adds donkeywork-agents-api audience to access tokens for MCP/DCR clients",
    "protocol": "openid-connect",
    "attributes": {
        "include.in.token.scope": "false",
        "display.on.consent.screen": "false"
    }
}
```

### 3. Added Audience Protocol Mapper to `mcp-audience` Scope

**Why:** This is the actual mechanism that injects the `donkeywork-agents-api` value into the `aud` claim of access tokens.

**What was done:**
- Added an `oidc-audience-mapper` protocol mapper named `donkeywork-agents-api-audience` to the `mcp-audience` scope
- Adds `donkeywork-agents-api` to the access token `aud` claim
- Not added to ID tokens
- Added to introspection tokens

**API call:**
```json
POST /admin/realms/Agents/client-scopes/{scope-id}/protocol-mappers/models
{
    "name": "donkeywork-agents-api-audience",
    "protocol": "openid-connect",
    "protocolMapper": "oidc-audience-mapper",
    "config": {
        "included.custom.audience": "donkeywork-agents-api",
        "id.token.claim": "false",
        "access.token.claim": "true",
        "lightweight.claim": "false",
        "introspection.token.claim": "true"
    }
}
```

### 4. Added `mcp-audience` as Realm Default Client Scope

**Why:** Making it a realm default means every client in the Agents realm (including future DCR-registered clients) automatically gets this scope. No per-client configuration needed.

**API call:**
```
PUT /admin/realms/Agents/default-default-client-scopes/{scope-id}
```

## How It Works End-to-End

```
MCP Client (ChatGPT/Claude)          DonkeyWork MCP Server          Keycloak
         |                                    |                        |
         |  1. POST / (no auth)               |                        |
         | ---------------------------------> |                        |
         |                                    |                        |
         |  2. 401 + WWW-Authenticate:        |                        |
         |     Bearer resource_metadata=      |                        |
         |     ".../.well-known/              |                        |
         |      oauth-protected-resource"     |                        |
         | <--------------------------------- |                        |
         |                                    |                        |
         |  3. GET /.well-known/              |                        |
         |     oauth-protected-resource       |                        |
         | ---------------------------------> |                        |
         |                                    |                        |
         |  4. { authorization_servers:       |                        |
         |     ["https://auth.donkeywork.dev  |                        |
         |      /realms/Agents"] }            |                        |
         | <--------------------------------- |                        |
         |                                    |                        |
         |  5. GET /realms/Agents/            |                        |
         |     .well-known/openid-config      |                        |
         | ---------------------------------------------------->      |
         |                                    |                        |
         |  6. OpenID config (with DCR URL)   |                        |
         | <----------------------------------------------------      |
         |                                    |                        |
         |  7. POST /realms/Agents/           |                        |
         |     clients-registrations/         |                        |
         |     openid-connect (DCR)           |                        |
         | ---------------------------------------------------->      |
         |                                    |                        |
         |  8. { client_id, client_secret }   |                        |
         | <----------------------------------------------------      |
         |                                    |                        |
         |  9. OAuth authorize + token flow   |                        |
         | ---------------------------------------------------->      |
         |                                    |                        |
         |  10. Access token                  |                        |
         |      (aud: donkeywork-agents-api)  |                        |
         | <----------------------------------------------------      |
         |                                    |                        |
         |  11. POST / (with Bearer token)    |                        |
         | ---------------------------------> |                        |
         |                                    |                        |
         |  12. MCP response                  |                        |
         | <--------------------------------- |                        |
```

## Code Changes (for reference)

Two files were modified alongside these Keycloak changes:

| File | Change |
|------|--------|
| `src/identity/DonkeyWork.Agents.Identity.Api/DependencyInjection.cs` | Added `.AddMcp()` to auth builder with `ProtectedResourceMetadata` pointing to Keycloak |
| `src/DonkeyWork.Agents.Api/Program.cs` | Changed `MapMcp()` to use `McpAuthenticationDefaults.AuthenticationScheme` for 401 challenges |

## Verification

| Check | URL/Command |
|-------|-------------|
| Resource metadata | `GET https://mcp.donkeywork.dev/.well-known/oauth-protected-resource` |
| 401 challenge header | `POST https://mcp.donkeywork.dev/` (no auth) should return `WWW-Authenticate: Bearer resource_metadata="..."` |
| DCR endpoint | `POST https://auth.donkeywork.dev/realms/Agents/clients-registrations/openid-connect` |
| End-to-end | Add `https://mcp.donkeywork.dev` as MCP server in ChatGPT or Claude |

## Reverting

To undo the Keycloak changes:

1. **Re-add Trusted Hosts policy:** Realm Settings > Client Registration > Client Registration Policies > Add "trusted-hosts" provider
2. **Remove default scope assignment:** Realm Settings > Client Scopes > Default Client Scopes > Remove `mcp-audience` from defaults
3. **Delete the scope:** Client Scopes > `mcp-audience` > Delete
