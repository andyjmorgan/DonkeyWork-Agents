# API Integration - Actions API ✓

## Summary

Created the Actions API module with REST endpoints for action node management. The frontend now loads action schemas dynamically from the backend instead of static JSON files.

## Changes Made

### 1. Created Actions API Module

**New Files Created:**

#### Backend API
- `src/actions/DonkeyWork.Agents.Actions.Api/Controllers/ActionsController.cs`
  - `GET /api/v1/actions/schemas` - Returns all action schemas
  - TODO: `POST /api/v1/actions/execute` (Phase 4)
  - TODO: `POST /api/v1/actions/validate` (Phase 4)

- `src/actions/DonkeyWork.Agents.Actions.Api/DependencyInjection.cs`
  - Registers `IActionSchemaService`, `IExpressionEngine`, `IParameterResolver`

#### API Models
- `src/actions/DonkeyWork.Agents.Actions.Contracts/Models/Api/GetSchemasResponseV1.cs`
  - Response model for schemas endpoint

---

### 2. Integrated with Main API

**Modified Files:**

#### Main API Registration
- `src/DonkeyWork.Agents.Api/Program.cs`
  - Added `using DonkeyWork.Agents.Actions.Api`
  - Added `builder.Services.AddActionsApi()` registration

- `src/DonkeyWork.Agents.Api/DonkeyWork.Agents.Api.csproj`
  - Added project reference to Actions.Api

#### Actions API Project
- `src/actions/DonkeyWork.Agents.Actions.Api/DonkeyWork.Agents.Actions.Api.csproj`
  - Added `Asp.Versioning.Mvc` package for API versioning

---

### 3. Updated Frontend to Use API

**Modified Files:**

- `src/frontend/src/hooks/useActions.ts`
  - Changed from static JSON import to async API fetch
  - Fetches from `/api/v1/actions/schemas`
  - Returns loading state while fetching
  - Handles errors gracefully

**Old Approach (Static JSON):**
```typescript
import actionsSchemaRaw from '@/schemas/actions.json'
const allActions = actionsSchemaRaw as unknown as ActionNodeSchema[]
```

**New Approach (Dynamic API):**
```typescript
const response = await fetch('/api/v1/actions/schemas')
const data = await response.json()
const enabledActions = data.schemas.filter(action => action.enabled)
setActions(enabledActions)
```

---

## API Endpoint Details

### GET /api/v1/actions/schemas

**Description**: Returns all available action node schemas with parameter definitions

**URL**: `GET /api/v1/actions/schemas`

**Response**: `200 OK`

```json
{
  "schemas": [
    {
      "actionType": "http_request",
      "displayName": "HTTP Request",
      "category": "Communication",
      "group": "HTTP",
      "icon": "globe",
      "description": "Make HTTP requests to external APIs",
      "maxInputs": -1,
      "maxOutputs": -1,
      "enabled": true,
      "parameters": [
        {
          "name": "method",
          "displayName": "Method",
          "description": "HTTP method to use",
          "type": "enum",
          "required": true,
          "defaultValue": "GET",
          "supportsVariables": false,
          "controlType": "dropdown",
          "options": [
            { "label": "GET", "value": "GET" },
            { "label": "POST", "value": "POST" },
            { "label": "PUT", "value": "PUT" },
            { "label": "DELETE", "value": "DELETE" },
            { "label": "PATCH", "value": "PATCH" }
          ],
          "resolvable": false
        },
        {
          "name": "url",
          "displayName": "URL",
          "description": "The URL to send the request to",
          "type": "string",
          "required": true,
          "defaultValue": "",
          "supportsVariables": true,
          "controlType": "text",
          "resolvable": true
        }
        // ... more parameters
      ]
    }
  ],
  "count": 1
}
```

---

## Architecture Flow

### Development Time (Build)

```
1. Developer writes C# action parameters with attributes
   ↓
2. dotnet build runs
   ↓
3. SchemaGenerator generates actions.json
   ↓
4. JSON saved to frontend/src/schemas/ (fallback only)
```

### Runtime (API Request)

```
1. Frontend: useActions() hook mounts
   ↓
2. Fetch GET /api/v1/actions/schemas
   ↓
3. Backend: ActionsController receives request
   ↓
4. ActionSchemaService scans Actions.Core assembly
   ↓
5. Generates schemas from [ActionNode] attributes
   ↓
6. Returns GetSchemasResponseV1
   ↓
7. Frontend: Receives and stores schemas
   ↓
8. NodePalette renders action nodes dynamically
```

---

## Service Registration

### DependencyInjection.cs

```csharp
public static IServiceCollection AddActionsApi(this IServiceCollection services)
{
    // Register core services
    services.AddScoped<IActionSchemaService, ActionSchemaService>();
    services.AddScoped<IExpressionEngine, ScribanExpressionEngine>();
    services.AddScoped<IParameterResolver, ParameterResolverService>();

    return services;
}
```

### Program.cs

```csharp
// Add Actions module
builder.Services.AddActionsApi();
```

---

## Benefits of API Approach

### 1. Dynamic Schema Loading
- No frontend rebuild needed when adding new actions
- Schemas generated fresh on each request
- Always reflects current backend state

### 2. Environment-Specific Schemas
- Different actions can be enabled per environment
- Configuration-driven action availability
- Easy A/B testing of new actions

### 3. Consistency
- Single source of truth (C# attributes)
- No sync issues between build-time JSON and runtime
- Backend controls what's available

### 4. Future-Proof
- Easy to add filtering (by category, permissions, etc.)
- Can add metadata (version, deprecation warnings)
- Supports feature flags and gradual rollouts

---

## Testing the API

### 1. Start Backend

```bash
cd /Users/andrewmorgan/Personal/source/DonkeyWork-Agents
dotnet run --project src/DonkeyWork.Agents.Api/DonkeyWork.Agents.Api.csproj
```

### 2. Test Endpoint

```bash
# Using curl
curl http://localhost:5050/api/v1/actions/schemas | jq

# Expected response
{
  "schemas": [...],
  "count": 1
}
```

### 3. Open Scalar Documentation

Navigate to `http://localhost:5050/scalar/v1` to see the API docs including the new Actions endpoints.

### 4. Test Frontend Integration

```bash
cd src/frontend
npm run dev

# Open browser to http://localhost:5173
# Navigate to agent editor
# Check browser console for:
# - Fetch request to /api/v1/actions/schemas
# - Successful response with schemas
# - Actions section populated in NodePalette
```

---

## Known Limitations

### 1. No Caching

Currently, the API generates schemas on every request by scanning the assembly. For production, consider:
- Adding response caching with `[ResponseCache]` attribute
- Using memory cache for schema generation
- Versioning schemas to enable long-term browser caching

**Example:**
```csharp
[HttpGet("schemas")]
[ResponseCache(Duration = 300)] // Cache for 5 minutes
public IActionResult GetSchemas() { ... }
```

### 2. No Authentication

The schemas endpoint is currently unauthenticated. For production:
- Add `[Authorize]` attribute
- Consider if schemas should be public or user-specific
- May need role-based access for certain actions

### 3. No Assembly Filtering

Currently scans only `Actions.Core` assembly. Future enhancement:
- Scan multiple assemblies
- Plugin system for third-party actions
- Dynamic action registration

---

## Future Endpoints (Phase 4)

### POST /api/v1/actions/execute

Execute an action with resolved parameters.

**Request:**
```json
{
  "actionType": "http_request",
  "parameters": {
    "method": "POST",
    "url": "https://api.example.com/data",
    "body": "{\"key\": \"value\"}"
  },
  "context": {
    "variables": { "apiKey": "secret123" },
    "nodeOutputs": { "start": { "input": "test" } }
  }
}
```

**Response:**
```json
{
  "success": true,
  "result": {
    "statusCode": 200,
    "body": "{\"success\": true}"
  },
  "executionTime": 234
}
```

### POST /api/v1/actions/validate

Validate action parameters before execution.

**Request:**
```json
{
  "actionType": "http_request",
  "parameters": {
    "method": "POST",
    "url": "https://api.example.com/data"
  }
}
```

**Response:**
```json
{
  "valid": true,
  "errors": []
}
```

Or with validation errors:
```json
{
  "valid": false,
  "errors": [
    {
      "field": "body",
      "message": "Body is required for POST requests"
    }
  ]
}
```

---

## Files Created

### Backend
1. `src/actions/DonkeyWork.Agents.Actions.Api/Controllers/ActionsController.cs` (62 lines)
2. `src/actions/DonkeyWork.Agents.Actions.Api/DependencyInjection.cs` (22 lines)
3. `src/actions/DonkeyWork.Agents.Actions.Contracts/Models/Api/GetSchemasResponseV1.cs` (18 lines)

### Configuration
4. Modified: `src/DonkeyWork.Agents.Api/Program.cs` (added Actions API registration)
5. Modified: `src/DonkeyWork.Agents.Api/DonkeyWork.Agents.Api.csproj` (added project reference)
6. Modified: `src/actions/DonkeyWork.Agents.Actions.Api/DonkeyWork.Agents.Actions.Api.csproj` (added packages)

### Frontend
7. Modified: `src/frontend/src/hooks/useActions.ts` (changed to async API fetch)

**Total: 3 new files, 4 modified files**

---

## Status

✅ Actions API module created
✅ GET /api/v1/actions/schemas endpoint working
✅ Frontend integrated with API
✅ Dynamic schema loading
✅ Build succeeds with no errors
✅ Ready for Scalar API documentation

⏳ POST /api/v1/actions/execute (Phase 4)
⏳ POST /api/v1/actions/validate (Phase 4)

**Next**: Option 3 - Add More Action Nodes (Email, Slack, Database, etc.)

---

**Completion Date**: 2026-01-24
**Status**: API Integration Complete
**Endpoint**: `GET /api/v1/actions/schemas`
