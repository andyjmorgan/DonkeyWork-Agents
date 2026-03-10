using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Contracts.Enums;

/// <summary>
/// Specifies the level of reasoning effort for models that support extended thinking.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReasoningEffort
{
    None = 0,
    Low,
    Medium,
    High
}
