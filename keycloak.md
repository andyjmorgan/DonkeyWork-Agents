# Keycloak Admin API Reference

Base URL: `https://auth.donkeywork.dev`

## Authentication

```bash
# Get admin token (URL-encode special chars in password)
curl -s -X POST "{base}/realms/master/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d 'username=admin&password={url-encoded}&grant_type=password&client_id=admin-cli'
```

Response: `{ "access_token": "...", "expires_in": 60 }`

## Realms

```bash
# List realms
GET /admin/realms

# Get realm details
GET /admin/realms/{realm}

# Key fields: realm, enabled, registrationAllowed, accessTokenLifespan, ssoSessionIdleTimeout

# Update realm settings
PUT /admin/realms/{realm}
Content-Type: application/json
{
  "realm": "{realm}",
  "revokeRefreshToken": true,
  "refreshTokenMaxReuse": 0
}
```

## Clients

```bash
# List clients
GET /admin/realms/{realm}/clients

# Get client by clientId
GET /admin/realms/{realm}/clients?clientId={clientId}

# Create public client (SPA/mobile)
POST /admin/realms/{realm}/clients
Content-Type: application/json
{
  "clientId": "my-client",
  "enabled": true,
  "publicClient": true,
  "standardFlowEnabled": true,
  "directAccessGrantsEnabled": false,
  "protocol": "openid-connect",
  "redirectUris": ["http://localhost:*", "https://example.com/*"],
  "webOrigins": ["+"],
  "attributes": {
    "pkce.code.challenge.method": "S256"
  }
}

# Response: 201 Created (empty body, Location header has client URL)
```

## Identity Providers

```bash
# List identity providers
GET /admin/realms/{realm}/identity-provider/instances

# Create identity provider
POST /admin/realms/{realm}/identity-provider/instances
```

---

## Current Configuration

### Realm: Agents
- Access token lifespan: 300s (5 min)
- SSO session idle: 1800s (30 min)
- SSO session max: 36000s (10 hr)
- Offline session idle: 2592000s (30 days)
- Offline session max: 5184000s (60 days)
- Refresh token rotation: enabled (revokeRefreshToken: true)
- Refresh token max reuse: 0 (single use)

### GitHub IdP
- Alias: `github`
- Client ID: `Ov23liuDfPdmMjef6obO`
- Callback URL: `https://auth.donkeywork.dev/realms/Agents/broker/github/endpoint`

### Client: donkeywork-agents-api
- ID: `104d6fbf-c946-49a4-9fba-5fe7653f8f85`
- Public client (no secret)
- PKCE required (S256)
- Redirect URIs: `http://localhost:*`, `https://*.donkeywork.dev/*`
- Web origins: `+` (same as redirect URIs)

## OIDC Endpoints (Agents realm)

```
Authorization: https://auth.donkeywork.dev/realms/Agents/protocol/openid-connect/auth
Token:         https://auth.donkeywork.dev/realms/Agents/protocol/openid-connect/token
UserInfo:      https://auth.donkeywork.dev/realms/Agents/protocol/openid-connect/userinfo
JWKS:          https://auth.donkeywork.dev/realms/Agents/protocol/openid-connect/certs
Discovery:     https://auth.donkeywork.dev/realms/Agents/.well-known/openid-configuration
```

## API Token Validation

The API validates JWT tokens using:
- Authority: `https://auth.donkeywork.dev/realms/Agents`
- Audience: `donkeywork-agents-api` (validated via `azp` claim, not `aud`)
- JWKS fetched from discovery endpoint

**Important:** Keycloak sets `aud: "account"` by default. The API uses a custom audience validator that checks the `azp` (authorized party) claim instead, which contains the client ID.

Claims extracted:
- `sub` → UserId (must be valid GUID)
- `email` → Email
- `name` → Display name
- `preferred_username` → Username

## Test Auth Flow

The API includes test endpoints for OAuth2 + PKCE flow:

```bash
# 1. Start login (redirects to Keycloak)
GET http://localhost:5247/api/v1/auth/login

# 2. After auth, callback returns tokens + user info
GET http://localhost:5247/api/v1/auth/callback?code=...

# 3. Use access token to call protected endpoints
curl -H "Authorization: Bearer {access_token}" http://localhost:5247/api/v1/me
```
