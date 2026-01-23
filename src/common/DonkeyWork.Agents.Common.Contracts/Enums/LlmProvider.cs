using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Contracts.Enums;

/// <summary>
/// Supported LLM providers.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LlmProvider
{
    OpenAI,
    Anthropic,
    Google
}
