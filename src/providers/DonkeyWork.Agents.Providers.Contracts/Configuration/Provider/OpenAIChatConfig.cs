using DonkeyWork.Agents.Common.Sdk.Attributes;
using DonkeyWork.Agents.Common.Sdk.Types;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Provider;

/// <summary>
/// OpenAI-specific configuration extending the base chat model config.
/// </summary>
public sealed class OpenAIChatConfig : ChatModelConfig
{
    [ConfigurableField(Label = "Frequency Penalty", Description = "Reduces repetition of token sequences (-2.0 to 2.0)", Order = 20, Group = "Penalties")]
    [Tab("Advanced")]
    [Slider(Min = -2.0, Max = 2.0, Step = 0.1, Default = 0)]
    public Resolvable<double>? FrequencyPenalty { get; init; }

    [ConfigurableField(Label = "Presence Penalty", Description = "Encourages discussing new topics (-2.0 to 2.0)", Order = 30, Group = "Penalties")]
    [Tab("Advanced")]
    [Slider(Min = -2.0, Max = 2.0, Step = 0.1, Default = 0)]
    public Resolvable<double>? PresencePenalty { get; init; }
}
