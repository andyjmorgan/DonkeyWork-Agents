using DonkeyWork.Agents.Providers.Contracts.Attributes;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Provider;

/// <summary>
/// Anthropic-specific configuration extending the base chat model config.
/// </summary>
public sealed class AnthropicChatConfig : ChatModelConfig
{
    [ConfigField(Label = "Thinking Budget", Description = "Max tokens for extended thinking", Order = 41, Group = "Reasoning")]
    [RequiresCapability(Capability = "Reasoning")]
    [RangeConstraint(Min = 1024, Max = 128000, DefaultValue = 10000)]
    public int? ThinkingBudget { get; init; }
}
