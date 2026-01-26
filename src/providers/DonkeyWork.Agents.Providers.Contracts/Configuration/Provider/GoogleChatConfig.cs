using DonkeyWork.Agents.Providers.Contracts.Attributes;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Provider;

/// <summary>
/// Google-specific configuration extending the base chat model config.
/// </summary>
public sealed class GoogleChatConfig : ChatModelConfig
{
    [ConfigField(Label = "Top K", Description = "Number of highest probability tokens to consider", Order = 35, Group = "Advanced")]
    [RangeConstraint(Min = 1, Max = 100, DefaultValue = 40)]
    public int? TopK { get; init; }
}
