using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Providers.Contracts.Models;

public sealed class ModelSupports
{
    [JsonPropertyName("vision")]
    public bool Vision { get; init; }

    [JsonPropertyName("audio_input")]
    public bool AudioInput { get; init; }

    [JsonPropertyName("audio_output")]
    public bool AudioOutput { get; init; }

    [JsonPropertyName("function_calling")]
    public bool FunctionCalling { get; init; }

    [JsonPropertyName("tool_choice")]
    public bool ToolChoice { get; init; }

    [JsonPropertyName("prompt_caching")]
    public bool PromptCaching { get; init; }

    [JsonPropertyName("reasoning")]
    public bool Reasoning { get; init; }

    [JsonPropertyName("image_output")]
    public bool ImageOutput { get; init; }

    [JsonPropertyName("streaming")]
    public bool Streaming { get; init; }
}
