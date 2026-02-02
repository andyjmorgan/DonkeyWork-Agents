using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations.ProviderConfigs;

/// <summary>
/// Reasoning effort level for OpenAI models that support it.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReasoningEffort
{
    /// <summary>
    /// Low reasoning effort - faster but less thorough.
    /// </summary>
    Low,

    /// <summary>
    /// Medium reasoning effort - balanced performance.
    /// </summary>
    Medium,

    /// <summary>
    /// High reasoning effort - slower but more thorough.
    /// </summary>
    High
}
