using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Base.Enums;

/// <summary>
/// Specifies the level of reasoning effort for models that support extended thinking.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReasoningEffort
{
    Low,
    Medium,
    High
}
