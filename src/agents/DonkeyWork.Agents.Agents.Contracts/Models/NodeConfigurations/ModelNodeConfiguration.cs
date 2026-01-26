using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;

/// <summary>
/// Configuration for a Model node that calls an LLM.
/// </summary>
public sealed class ModelNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// LLM provider.
    /// </summary>
    [JsonPropertyName("provider")]
    public required LlmProvider Provider { get; init; }

    /// <summary>
    /// Model identifier.
    /// </summary>
    [JsonPropertyName("modelId")]
    public required string ModelId { get; init; }

    /// <summary>
    /// Foreign key to the credential (ExternalApiKey).
    /// </summary>
    [JsonPropertyName("credentialId")]
    public required Guid CredentialId { get; init; }

    /// <summary>
    /// Optional system prompt template (Scriban).
    /// </summary>
    [JsonPropertyName("systemPrompt")]
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// User message template (Scriban).
    /// </summary>
    [JsonPropertyName("userMessage")]
    public required string UserMessage { get; init; }

    /// <summary>
    /// Temperature for sampling (0.0 to 2.0). Optional, uses provider default if not set.
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }

    /// <summary>
    /// Maximum tokens to generate. Optional, uses provider default if not set.
    /// </summary>
    [JsonPropertyName("maxTokens")]
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Top-p sampling parameter (0.0 to 1.0). Optional, uses provider default if not set.
    /// </summary>
    [JsonPropertyName("topP")]
    public double? TopP { get; init; }
}
