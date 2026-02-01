using DonkeyWork.Agents.Common.Sdk.Attributes;
using DonkeyWork.Agents.Common.Sdk.Types;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Provider;

/// <summary>
/// Anthropic-specific configuration extending the base chat model config.
/// </summary>
public sealed class AnthropicChatConfig : ChatModelConfig
{
    [ConfigurableField(Label = "Thinking Budget", Description = "Max tokens for extended thinking", Order = 20, Group = "Settings")]
    [Tab("Reasoning")]
    [RequiresCapability(Capability = "Reasoning")]
    [ReliesUpon(FieldName = nameof(EnableReasoning), Value = true)]
    [RangeConstraint(Min = 1024, Max = 128000, Default = 10000)]
    public Resolvable<int>? ThinkingBudget { get; init; }
}
