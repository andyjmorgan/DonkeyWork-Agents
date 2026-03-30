# Identity Module

JWT Bearer authentication with Keycloak.

## Structure

```
identity/
├── DonkeyWork.Agents.Identity.Contracts/
│   ├── Models/
│   │   ├── GetMeResponseV1.cs
│   │   ├── LoginCallbackResponseV1.cs
│   │   └── KeycloakUserInfo.cs
│   └── Services/
│       ├── IIdentityContext.cs
│       └── IKeycloakService.cs
├── DonkeyWork.Agents.Identity.Core/
│   └── Services/
│       ├── IdentityContext.cs
│       └── KeycloakService.cs
└── DonkeyWork.Agents.Identity.Api/
    ├── Authentication/
    │   └── ApiKeyAuthenticationHandler.cs  # API key auth scheme
    ├── Controllers/
    │   ├── AuthController.cs      # OAuth2 + PKCE test flow
    │   └── MeController.cs        # Get authenticated user
    ├── McpTools/
    │   └── IdentityTools.cs       # MCP tool for identity lookup
    ├── Options/
    │   └── KeycloakOptions.cs
    └── DependencyInjection.cs
```

## Configuration

```json
{
  "Keycloak": {
    "Authority": "https://auth.donkeywork.dev/realms/Agents",
    "Audience": "donkeywork-agents-api",
    "RequireHttpsMetadata": true
  }
}
```

## Usage

```csharp
// In Program.cs
builder.Services.AddIdentityApi(builder.Configuration);

// In controllers - inject IIdentityContext
public class MyController(IIdentityContext identity) : ControllerBase
{
    [Authorize]
    public IActionResult Get()
    {
        var userId = identity.UserId;  // GUID from Keycloak
        var email = identity.Email;
        // ...
    }
}
```

## Endpoints

| Endpoint | Auth | Description |
|----------|------|-------------|
| `GET /api/v1/auth/login` | No | Redirects to Keycloak |
| `GET /api/v1/auth/callback` | No | Exchanges code for tokens |
| `GET /api/v1/me` | Yes | Returns authenticated user info |

## Notes

- Uses `azp` claim for audience validation (Keycloak default `aud` is "account")
- UserId must be valid GUID (fails auth if not)
- PKCE (S256) required for auth flow
