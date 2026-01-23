# Providers Module

This module manages LLM provider definitions, model catalogs, and provider client abstractions.

## Supported Providers

- **OpenAI**: GPT-5, GPT-5 mini, GPT-5 nano, GPT Image 1.5, GPT-4o mini TTS, Sora 2
- **Anthropic**: Claude Opus 4.5, Claude Sonnet 4.5, Claude Haiku 4.5
- **Google**: Gemini 2.5 Pro/Flash, Gemini 3 Pro/Flash, Veo 3.1

## Provider Client Types

Models are categorized by their input/output capabilities:

| Client Type | Input | Output | Examples |
|-------------|-------|--------|----------|
| MultimodalInput | Text, Image, Audio | Text | GPT-5, Claude 4.5, Gemini |
| MultimodalDuplex | Text, Image, Audio | Text, Image | Gemini models |
| ImageOutput | Text, Image | Image | GPT Image 1.5 |
| AudioOutput | Text | Audio | GPT-4o mini TTS |
| VideoOutput | Text, Image | Video + Audio | Sora 2, Veo 3.1 |

## Model Catalog

Models are defined in `Data/models.json` with the following structure:

- `id`: Unique model identifier
- `name`: Display name
- `provider`: OpenAI, Anthropic, or Google
- `mode`: Chat, ImageGeneration, AudioGeneration, VideoGeneration
- `max_input_tokens`: Maximum input context window
- `max_output_tokens`: Maximum output tokens
- `input_cost_per_million_tokens`: Cost per 1M input tokens
- `output_cost_per_million_tokens`: Cost per 1M output tokens
- `supports`: Capability flags (vision, audio_input, audio_output, function_calling, etc.)
- `client_types`: Applicable provider client types

## Contracts

### IModelCatalogService

```csharp
IReadOnlyList<ModelDefinition> GetAllModels();
ModelDefinition? GetModelById(string id);
IReadOnlyList<ModelDefinition> GetModelsByProvider(LlmProvider provider);
IReadOnlyList<ModelDefinition> GetModelsByClientType(ProviderClientType clientType);
IReadOnlyList<ModelDefinition> GetModelsByMode(ModelMode mode);
```
