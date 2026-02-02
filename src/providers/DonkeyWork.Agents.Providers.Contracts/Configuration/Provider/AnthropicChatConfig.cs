using DonkeyWork.Agents.Common.Sdk.Attributes;
using DonkeyWork.Agents.Common.Sdk.Types;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base.Enums;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Provider;

/// <summary>
/// Anthropic-specific configuration extending the base chat model config.
/// </summary>
public sealed class AnthropicChatConfig : ChatModelConfig
{
    // === Reasoning (Reasoning Tab - Anthropic specific) ===
    [ConfigurableField(Label = "Enable Reasoning", Description = "Enable extended thinking capabilities", Order = 0)]
    [Tab("Reasoning", Order = 2, Icon = "brain")]
    [RequiresCapability(Capability = "Reasoning")]
    public Resolvable<bool>? EnableReasoning { get; init; }

    [ConfigurableField(Label = "Reasoning Effort", Description = "How much reasoning to apply", Order = 10, Group = "Settings")]
    [Tab("Reasoning")]
    [RequiresCapability(Capability = "Reasoning")]
    [ReliesUpon(FieldName = nameof(EnableReasoning), Value = true)]
    [SelectOptions(Default = "Medium")]
    public Resolvable<ReasoningEffort>? ReasoningEffort { get; init; }

    [ConfigurableField(Label = "Thinking Budget", Description = "Max tokens for extended thinking", Order = 20, Group = "Settings")]
    [Tab("Reasoning")]
    [RequiresCapability(Capability = "Reasoning")]
    [ReliesUpon(FieldName = nameof(EnableReasoning), Value = true)]
    [RangeConstraint(Min = 1024, Max = 128000, Default = 10000)]
    public Resolvable<int>? ThinkingBudget { get; init; }
}
