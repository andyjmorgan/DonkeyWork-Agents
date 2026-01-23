using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModelMode
{
    Chat,
    Embedding,
    ImageGeneration,
    AudioGeneration,
    AudioTranscription,
    VideoGeneration
}
