# AI Providers Implementation Plan

## Overview

Replace the `PlaceholderAiClient` with real AI provider implementations for OpenAI, Anthropic, and Google Gemini using their official C# SDKs.

## SDKs

| Provider | NuGet Package | Version | Client Class | Streaming Method |
|----------|--------------|---------|-------------|-----------------|
| OpenAI | `OpenAI` | 2.* | `ChatClient` | `CompleteChatStreamingAsync()` |
| Anthropic | `Anthropic` | 12.* | `AnthropicClient` | `client.Messages.CreateStreaming()` |
| Google | `Google_GenerativeAI` | 3.* | `GenerativeModel` | `StreamContentAsync()` |

## Architecture

```
IAiClientFactory (routes by LlmProvider enum)
├── OpenAiClient : IAiClient
├── AnthropicAiClient : IAiClient
└── GoogleAiClient : IAiClient
```

### Changes Required

1. **PipelineModelConfig** - Add `ApiKey` property (public contract)
2. **InternalModelConfig** - Add `ApiKey` property (internal)
3. **ModelPipeline** - Thread `ApiKey` from public to internal config
4. **Three provider clients** - Each implements `IAiClient`, maps internal messages/tools to SDK types, streams responses back as `ModelResponseBase`
5. **AiClientFactory** - Routes to correct provider by `LlmProvider` enum
6. **DI Registration** - Replace `PlaceholderAiClientFactory` with real factory

### Provider Client Responsibilities

Each client must:
- Convert `InternalMessage` list → SDK-specific message format
- Convert `InternalToolDefinition` list → SDK-specific tool format
- Map `providerParameters` (temperature, maxTokens, topP, etc.) → SDK options
- Call the SDK streaming method
- Yield normalized `ModelResponseBase` subtypes:
  - `ModelResponseBlockStart/End` for content block lifecycle
  - `ModelResponseTextContent` for text chunks
  - `ModelResponseThinkingContent` for reasoning (Anthropic/OpenAI)
  - `ModelResponseToolCall` for tool requests
  - `ModelResponseUsage` for token counts
  - `ModelResponseStreamEnd` with stop reason

### Message Mapping

| Internal | OpenAI | Anthropic | Google |
|----------|--------|-----------|--------|
| System message | `SystemChatMessage` | `system` parameter | system instruction |
| User message | `UserChatMessage` | `MessageParam(Role.User)` | `Content(Role.User)` |
| Assistant message | `AssistantChatMessage` | `MessageParam(Role.Assistant)` | `Content(Role.Model)` |

### Provider Parameters

| Parameter | OpenAI | Anthropic | Google |
|-----------|--------|-----------|--------|
| temperature | `ChatCompletionOptions.Temperature` | `Temperature` | `GenerationConfig.Temperature` |
| max_tokens | `ChatCompletionOptions.MaxOutputTokenCount` | `MaxTokens` | `GenerationConfig.MaxOutputTokens` |
| top_p | `ChatCompletionOptions.TopP` | `TopP` | `GenerationConfig.TopP` |
| thinking_budget | N/A | `Thinking.BudgetTokens` | N/A |
| frequency_penalty | `ChatCompletionOptions.FrequencyPenalty` | N/A | N/A |
| presence_penalty | `ChatCompletionOptions.PresencePenalty` | N/A | N/A |
| top_k | N/A | N/A | `GenerationConfig.TopK` |

## File Structure

```
src/providers/DonkeyWork.Agents.Providers.Core/
├── Providers/
│   ├── IAiClient.cs (existing)
│   ├── IAiClientFactory.cs (existing)
│   ├── AiClientFactory.cs (new - replaces PlaceholderAiClientFactory)
│   ├── OpenAi/
│   │   └── OpenAiClient.cs
│   ├── Anthropic/
│   │   └── AnthropicAiClient.cs
│   └── Google/
│       └── GoogleAiClient.cs
```
