using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations.ProviderConfigs;

/// <summary>
/// Anthropic-specific provider configuration.
/// </summary>
public sealed class AnthropicProviderConfig : ProviderConfig
{
    /// <summary>
    /// Top-K sampling parameter. Limits token selection to top K tokens.
    /// </summary>
    [JsonPropertyName("topK")]
    [ConfigurableField(Label = "Top K", ControlType = ControlType.Number, Order = 10)]
    [Tab("Anthropic", Order = 1)]
    public int? TopK { get; init; }

    /// <summary>
    /// Enable extended thinking for supported models (claude-opus-4-6, claude-sonnet-4-6).
    /// </summary>
    [JsonPropertyName("enableExtendedThinking")]
    [ConfigurableField(Label = "Enable Extended Thinking", ControlType = ControlType.Toggle, Order = 20)]
    [Tab("Anthropic", Order = 1)]
    [SupportedBy("claude-opus-4-6", "claude-sonnet-4-6")]
    [Group("Extended Thinking")]
    public bool? EnableExtendedThinking { get; init; }

    /// <summary>
    /// Budget for extended thinking in tokens (1024-128000).
    /// </summary>
    [JsonPropertyName("thinkingBudget")]
    [ConfigurableField(Label = "Thinking Budget", ControlType = ControlType.Number, Order = 30)]
    [Tab("Anthropic", Order = 1)]
    [SupportedBy("claude-opus-4-6", "claude-sonnet-4-6")]
    [ReliesUpon(FieldName = nameof(EnableExtendedThinking), Value = true)]
    [Group("Extended Thinking")]
    public int? ThinkingBudget { get; init; }

    /// <summary>
    /// Enable interleaved thinking for multi-turn conversations.
    /// </summary>
    [JsonPropertyName("interleavedThinking")]
    [ConfigurableField(Label = "Interleaved Thinking", ControlType = ControlType.Toggle, Order = 40)]
    [Tab("Anthropic", Order = 1)]
    [SupportedBy("claude-opus-4-6", "claude-sonnet-4-6")]
    [ReliesUpon(FieldName = nameof(EnableExtendedThinking), Value = true)]
    [Group("Extended Thinking")]
    public bool? InterleavedThinking { get; init; }
}
