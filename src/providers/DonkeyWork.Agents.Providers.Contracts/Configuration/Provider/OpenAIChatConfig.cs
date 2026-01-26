using DonkeyWork.Agents.Providers.Contracts.Attributes;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Provider;

/// <summary>
/// OpenAI-specific configuration extending the base chat model config.
/// </summary>
public sealed class OpenAIChatConfig : ChatModelConfig
{
    [ConfigField(Label = "Frequency Penalty", Description = "Reduces repetition of token sequences", Order = 50, Group = "Advanced")]
    [Slider(Min = -2.0, Max = 2.0, Step = 0.1, DefaultValue = 0)]
    public double? FrequencyPenalty { get; init; }

    [ConfigField(Label = "Presence Penalty", Description = "Encourages discussing new topics", Order = 51, Group = "Advanced")]
    [Slider(Min = -2.0, Max = 2.0, Step = 0.1, DefaultValue = 0)]
    public double? PresencePenalty { get; init; }
}
