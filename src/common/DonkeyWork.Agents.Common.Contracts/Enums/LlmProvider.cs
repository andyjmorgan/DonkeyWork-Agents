using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Contracts.Enums;

/// <summary>
/// Supported LLM providers.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LlmProvider
{
    /// <summary>
    /// Unknown/unset provider. Used as sentinel value for validation.
    /// </summary>
    Unknown = 0,
    OpenAI,
    Anthropic,
    Google
}
