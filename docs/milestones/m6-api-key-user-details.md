# M6: API Key User Details from Keycloak (Post-MVP)

> **Status: Post-MVP** — Enhancement to API key authentication.

## Overview

Enhance API key authentication to fetch full user details (email, name, username) from Keycloak Admin API, instead of only having the user ID.

## Background

Currently, when authenticating via API key:
1. The API key is validated and returns a `userId`
2. User details (email, name, username) are not fetched from Keycloak
3. The `IIdentityContext` only has the `UserId` populated

This milestone adds Keycloak Admin API integration to fetch the complete user profile during API key authentication.

## Goals

1. Configure service account credentials for Keycloak Admin API access
2. Implement user lookup by ID via Keycloak Admin API
3. Cache user details to minimize API calls
4. Populate full `IIdentityContext` during API key auth

## Deliverables

### Configuration

- [ ] Add `ServiceAccountClientId` to `KeycloakOptions`
- [ ] Add `ServiceAccountClientSecret` to `KeycloakOptions`
- [ ] Add `AdminApiUrl` to `KeycloakOptions` (derived from Authority if not set)

### Keycloak Service Enhancement

- [ ] Add `GetServiceAccountTokenAsync()` method
- [ ] Add `GetUserByIdAsync(Guid userId)` method using Admin API
- [ ] Handle token caching for service account
- [ ] Handle user details caching (already exists, needs enhancement)

### API Key Handler Updates

- [ ] Call `GetUserByIdAsync` when API key is validated
- [ ] Populate full user details in cache
- [ ] Add claims for email, name, username to `ClaimsPrincipal`

## API Flow

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   MCP Client    │    │   API Server    │    │    Keycloak     │
└────────┬────────┘    └────────┬────────┘    └────────┬────────┘
         │                      │                      │
         │  X-Api-Key: xxx      │                      │
         │ ───────────────────► │                      │
         │                      │                      │
         │                      │  Validate API Key    │
         │                      │  (get userId)        │
         │                      │                      │
         │                      │  Cache miss?         │
         │                      │                      │
         │                      │  Get service token   │
         │                      │ ───────────────────► │
         │                      │                      │
         │                      │  GET /admin/realms/  │
         │                      │  {realm}/users/{id}  │
         │                      │ ───────────────────► │
         │                      │                      │
         │                      │  User details        │
         │                      │ ◄─────────────────── │
         │                      │                      │
         │  Authenticated       │                      │
         │  (full identity)     │                      │
         │ ◄─────────────────── │                      │
```

## Keycloak Admin API

### Get User by ID

```
GET /admin/realms/{realm}/users/{userId}
Authorization: Bearer {service_account_token}
```

Response:
```json
{
  "id": "user-uuid",
  "username": "john.doe",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "emailVerified": true,
  "enabled": true
}
```

### Service Account Token

```
POST /realms/{realm}/protocol/openid-connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id={service_account_client_id}
&client_secret={service_account_client_secret}
```

## Keycloak Setup Requirements

1. Create a service account client in Keycloak
2. Enable "Service Accounts Enabled" on the client
3. Assign `view-users` role from `realm-management` client
4. Configure client credentials in application settings

## Caching Strategy

- Service account token: Cache until expiry (minus buffer)
- User details: Cache for 30-60 seconds (already implemented)

## Dependencies

- M2: MCP Server Native Tools (API key authentication exists)

## Current Workaround

The MCP server uses stateless mode (`HttpServerTransportOptions.Stateless = true`) which ensures `IIdentityContext` is resolved from `HttpContext.RequestServices`. This provides proper user isolation even without full user details.
