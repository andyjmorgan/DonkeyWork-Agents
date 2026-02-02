using DonkeyWork.Agents.Common.Sdk.Attributes;
using DonkeyWork.Agents.Common.Sdk.Types;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base.Enums;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Provider;

/// <summary>
/// OpenAI-specific configuration extending the base chat model config.
/// </summary>
public sealed class OpenAIChatConfig : ChatModelConfig
{
    // === Advanced Parameters (Advanced Tab) ===
    [ConfigurableField(Label = "Top P", Description = "Nucleus sampling threshold (0-1)", Order = 10, Group = "Sampling")]
    [Tab("Advanced", Order = 1, Icon = "sliders")]
    [Slider(Min = 0, Max = 1, Step = 0.05, Default = 1.0)]
    public Resolvable<double>? TopP { get; init; }

    [ConfigurableField(Label = "Frequency Penalty", Description = "Reduces repetition of token sequences (-2.0 to 2.0)", Order = 20, Group = "Penalties")]
    [Tab("Advanced")]
    [Slider(Min = -2.0, Max = 2.0, Step = 0.1, Default = 0)]
    public Resolvable<double>? FrequencyPenalty { get; init; }

    [ConfigurableField(Label = "Presence Penalty", Description = "Encourages discussing new topics (-2.0 to 2.0)", Order = 30, Group = "Penalties")]
    [Tab("Advanced")]
    [Slider(Min = -2.0, Max = 2.0, Step = 0.1, Default = 0)]
    public Resolvable<double>? PresencePenalty { get; init; }

    // === Reasoning (Reasoning Tab - OpenAI specific) ===
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
}
