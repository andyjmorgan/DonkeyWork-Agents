using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations.ProviderConfigs;

/// <summary>
/// OpenAI-specific provider configuration.
/// </summary>
public sealed class OpenAIProviderConfig : ProviderConfig
{
    /// <summary>
    /// Frequency penalty (-2 to 2). Positive values decrease repetition of token sequences.
    /// </summary>
    [JsonPropertyName("frequencyPenalty")]
    [ConfigurableField(Label = "Frequency Penalty", ControlType = ControlType.Slider, Order = 10)]
    [Tab("OpenAI", Order = 1)]
    [Slider(Min = -2, Max = 2, Step = 0.1, Default = 0)]
    public double? FrequencyPenalty { get; init; }

    /// <summary>
    /// Presence penalty (-2 to 2). Positive values increase likelihood of new topics.
    /// </summary>
    [JsonPropertyName("presencePenalty")]
    [ConfigurableField(Label = "Presence Penalty", ControlType = ControlType.Slider, Order = 20)]
    [Tab("OpenAI", Order = 1)]
    [Slider(Min = -2, Max = 2, Step = 0.1, Default = 0)]
    public double? PresencePenalty { get; init; }

    /// <summary>
    /// Reasoning effort level for models that support it (o3, o3-mini, o1).
    /// </summary>
    [JsonPropertyName("reasoningEffort")]
    [ConfigurableField(Label = "Reasoning Effort", ControlType = ControlType.Select, Order = 30)]
    [Tab("OpenAI", Order = 1)]
    [SupportedBy("o3", "o3-mini", "o1")]
    public ReasoningEffort? ReasoningEffort { get; init; }

    /// <summary>
    /// Enable JSON mode for structured output (gpt-5, gpt-4o, gpt-4-turbo).
    /// </summary>
    [JsonPropertyName("jsonMode")]
    [ConfigurableField(Label = "JSON Mode", ControlType = ControlType.Toggle, Order = 40)]
    [Tab("OpenAI", Order = 1)]
    [SupportedBy("gpt-5", "gpt-4o", "gpt-4-turbo")]
    public bool? JsonMode { get; init; }
}
