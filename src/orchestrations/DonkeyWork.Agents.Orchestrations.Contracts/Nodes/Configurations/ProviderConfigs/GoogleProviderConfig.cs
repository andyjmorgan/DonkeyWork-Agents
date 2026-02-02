using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations.ProviderConfigs;

/// <summary>
/// Google-specific provider configuration.
/// </summary>
public sealed class GoogleProviderConfig : ProviderConfig
{
    /// <summary>
    /// Top-K sampling parameter. Limits token selection to top K tokens.
    /// </summary>
    [JsonPropertyName("topK")]
    [ConfigurableField(Label = "Top K", ControlType = ControlType.Number, Order = 10)]
    [Tab("Google", Order = 1)]
    public int? TopK { get; init; }

    /// <summary>
    /// Number of response candidates to generate (1-8).
    /// </summary>
    [JsonPropertyName("candidateCount")]
    [ConfigurableField(Label = "Candidate Count", ControlType = ControlType.Number, Order = 20)]
    [Tab("Google", Order = 1)]
    public int? CandidateCount { get; init; }
}
