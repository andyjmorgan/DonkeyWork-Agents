# Providers Module

## Model Catalog (models.json)

The model catalog is a static JSON file that defines all available LLM models, their capabilities, token limits, and pricing.

### Location

```
src/providers/DonkeyWork.Agents.Providers.Contracts/Data/models.json
```

### How It's Loaded

- Embedded as a resource in `DonkeyWork.Agents.Providers.Contracts.dll`
- Loaded lazily on first access via `ModelCatalogService`
- Cached in memory for the lifetime of the application

```csharp
// ModelCatalogService.cs
private const string ModelsResourceName = "DonkeyWork.Agents.Providers.Contracts.Data.models.json";
private static readonly Lazy<IReadOnlyList<ModelDefinition>> LazyModels = new(LoadModels);
```

### Model Definition Structure

Each model entry has the following fields:

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier (e.g., "claude-opus-4-5", "gpt-5") |
| `name` | string | Display name (e.g., "Claude Opus 4.5") |
| `provider` | enum | LLM provider: "OpenAI", "Anthropic", "Google" |
| `mode` | enum | Model mode: "Chat", "Completion", "Embedding" |
| `max_input_tokens` | int | Maximum input context window |
| `max_output_tokens` | int | Maximum tokens the model can generate |
| `input_cost_per_million_tokens` | decimal | Cost per 1M input tokens (USD) |
| `output_cost_per_million_tokens` | decimal | Cost per 1M output tokens (USD) |
| `supports` | object | Capability flags (see below) |
| `client_types` | string[] | Supported client types: "MultimodalInput", "TextOnly" |
| `config_overrides` | object? | Optional per-model config field overrides |

### Supports Object

```json
{
  "vision": true,
  "audio_input": false,
  "audio_output": false,
  "function_calling": true,
  "tool_choice": true,
  "prompt_caching": true,
  "reasoning": true,
  "image_output": false,
  "streaming": true
}
```

### Config Overrides

Models can override default field constraints. Used by the frontend to adjust UI controls per model.

```json
{
  "config_overrides": {
    "temperature": { "max": 1.0 },
    "maxOutputTokens": { "max": 64000, "default": 8192 }
  }
}
```

### API Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /api/v1/models` | List all models |
| `GET /api/v1/models/{modelId}` | Get a specific model by ID |
| `GET /api/v1/models/{modelId}/config-schema` | Get config schema for a model |
| `GET /api/v1/models/config-schemas` | Get config schemas for all models |

### Frontend Usage

The frontend fetches model data to:

1. **Populate the node palette** - Groups models by provider
2. **Set dynamic slider limits** - Uses `max_output_tokens` for the Max Output Tokens slider
3. **Display model info** - Shows model name in properties panel

```typescript
// Frontend ModelDefinition interface (api.ts)
export interface ModelDefinition {
  id: string
  name: string
  provider: 'OpenAi' | 'Anthropic' | 'Google' | 'Azure'
  mode: string
  max_input_tokens: number
  max_output_tokens: number  // Used for slider max
  input_cost_per_million_tokens: number
  output_cost_per_million_tokens: number
}
```

### Adding a New Model

1. Add entry to `models.json`:
```json
{
  "id": "new-model-id",
  "name": "New Model Name",
  "provider": "OpenAI",
  "mode": "Chat",
  "max_input_tokens": 128000,
  "max_output_tokens": 16384,
  "input_cost_per_million_tokens": 1.00,
  "output_cost_per_million_tokens": 2.00,
  "supports": {
    "vision": true,
    "audio_input": false,
    "audio_output": false,
    "function_calling": true,
    "tool_choice": true,
    "prompt_caching": false,
    "reasoning": false,
    "image_output": false,
    "streaming": true
  },
  "client_types": ["MultimodalInput"]
}
```

2. Rebuild the project (the JSON is embedded at compile time)

3. Restart the backend

### Schema Validation

A JSON schema file exists for validation:
```
src/providers/DonkeyWork.Agents.Providers.Contracts/Data/models.schema.json
```

### Related Files

- `ModelDefinition.cs` - C# model class
- `ModelCatalog.cs` - Wrapper for the models array
- `ModelCatalogService.cs` - Service that loads and queries models
- `ModelsController.cs` - API endpoints
- `IModelCatalogService.cs` - Service interface
