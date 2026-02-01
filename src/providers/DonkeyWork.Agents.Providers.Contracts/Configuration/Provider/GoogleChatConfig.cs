using DonkeyWork.Agents.Common.Sdk.Attributes;
using DonkeyWork.Agents.Common.Sdk.Types;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Provider;

/// <summary>
/// Google-specific configuration extending the base chat model config.
/// </summary>
public sealed class GoogleChatConfig : ChatModelConfig
{
    [ConfigurableField(Label = "Top K", Description = "Number of highest probability tokens to consider", Order = 20)]
    [Tab("Advanced")]
    [RangeConstraint(Min = 1, Max = 100, Default = 40)]
    public Resolvable<int>? TopK { get; init; }
}
