using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CustomModelWireFormat
{
    AnthropicMessages,
    OpenAIResponses
}
