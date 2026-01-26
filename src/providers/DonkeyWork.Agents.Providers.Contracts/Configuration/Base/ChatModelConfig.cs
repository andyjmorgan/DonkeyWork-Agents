using DonkeyWork.Agents.Providers.Contracts.Attributes;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base.Enums;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

/// <summary>
/// Base configuration for chat/completion models.
/// </summary>
public class ChatModelConfig : IModelConfig
{
    [ConfigField(Label = "Temperature", Description = "Controls randomness (0=deterministic, higher=creative)", Order = 10)]
    [Slider(Min = 0, Max = 2, Step = 0.1, DefaultValue = 1.0)]
    public double? Temperature { get; init; }

    [ConfigField(Label = "Max Output Tokens", Description = "Maximum tokens to generate", Order = 20)]
    [RangeConstraint(Min = 1, Max = 128000, DefaultValue = 4096)]
    public int? MaxOutputTokens { get; init; }

    [ConfigField(Label = "Top P", Description = "Nucleus sampling threshold", Order = 30, Group = "Advanced")]
    [Slider(Min = 0, Max = 1, Step = 0.05, DefaultValue = 1.0)]
    public double? TopP { get; init; }

    [ConfigField(Label = "Reasoning Effort", Description = "How much reasoning to apply", Order = 40)]
    [RequiresCapability(Capability = "Reasoning")]
    [Select(DefaultValue = nameof(Enums.ReasoningEffort.Medium))]
    public ReasoningEffort? ReasoningEffort { get; init; }
}
