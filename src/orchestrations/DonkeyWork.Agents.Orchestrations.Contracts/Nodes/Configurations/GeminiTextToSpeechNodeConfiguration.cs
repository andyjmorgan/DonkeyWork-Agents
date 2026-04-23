using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the Gemini Text-to-Speech node.
/// Uses Google Gemini TTS models which support larger context windows (8K tokens).
/// </summary>
[Node(
    DisplayName = "Gemini Text to Speech",
    Description = "Generate speech audio from text using Google Gemini TTS",
    Category = "Audio",
    Icon = "volume-2",
    Color = "pink")]
public sealed class GeminiTextToSpeechNodeConfiguration : NodeConfiguration, IRequiresCredential
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.GeminiTextToSpeech;

    [JsonPropertyName("credentialId")]
    [ConfigurableField(Label = "Credential", ControlType = ControlType.Credential, Order = 10)]
    [Tab("Basic", Order = 1, Icon = "settings")]
    public required Guid CredentialId { get; init; }

    [JsonPropertyName("model")]
    [ConfigurableField(Label = "Model", ControlType = ControlType.Text, Order = 20)]
    [Tab("Basic", Order = 1)]
    [Immutable]
    public required string Model { get; init; }

    [JsonPropertyName("voice")]
    [ConfigurableField(Label = "Voice", ControlType = ControlType.Select, Order = 30)]
    [Tab("Basic", Order = 1)]
    [SelectOptions(
        "Achernar", "Achird", "Algenib", "Algieba", "Alnilam", "Aoede", "Autonoe",
        "Callirrhoe", "Charon", "Despina", "Enceladus", "Erinome", "Fenrir",
        "Gacrux", "Iapetus", "Kore", "Laomedeia", "Leda", "Orus",
        "Puck", "Pulcherrima", "Rasalgethi", "Sadachbia", "Sadaltager",
        "Schedar", "Sulafat", "Umbriel", "Vindemiatrix", "Zephyr", "Zubenelgenubi",
        Default = "Kore")]
    public required string Voice { get; init; }

    /// <summary>
    /// The text(s) to convert to speech. Must render to a JSON array of strings — each
    /// element is generated as a separate clip. Fan-out bounded by <see cref="MaxParallelism"/>.
    /// For a single clip: <c>["{{ Input.text | string.escape }}"]</c>.
    /// For chunked input: <c>{{ Steps.chunk_node.Chunks | to_json }}</c>.
    /// </summary>
    [JsonPropertyName("inputs")]
    [ConfigurableField(Label = "Inputs (JSON array)", ControlType = ControlType.TextArea, Order = 10, Required = true,
        Description = "JSON array of text chunks. Use {{ Steps.chunk_node.Chunks | to_json }} or [\"your text\"] for a single clip.")]
    [Tab("Content", Order = 2, Icon = "file-text")]
    [SupportVariables]
    public required string Inputs { get; init; }

    [JsonPropertyName("instructions")]
    [ConfigurableField(Label = "Voice Instructions", ControlType = ControlType.TextArea, Order = 20,
        Description = "Guide the voice style via natural language, e.g. 'Speak cheerfully with moderate pacing'.",
        Placeholder = "e.g. Speak in a warm, conversational tone.")]
    [Tab("Content", Order = 2)]
    [SupportVariables]
    public string? Instructions { get; init; }

    [JsonPropertyName("responseFormat")]
    [ConfigurableField(Label = "Format", ControlType = ControlType.Select, Order = 10)]
    [Tab("Advanced", Order = 3, Icon = "sliders")]
    [SelectOptions("mp3", "wav", "pcm", Default = "mp3")]
    public string ResponseFormat { get; init; } = "mp3";

    /// <summary>
    /// Maximum number of clips to generate in parallel. Bounded by provider rate limits.
    /// </summary>
    [JsonPropertyName("maxParallelism")]
    [ConfigurableField(Label = "Max Parallelism", ControlType = ControlType.Slider, Order = 20,
        Description = "Maximum concurrent provider calls when processing multiple chunks.")]
    [Tab("Advanced", Order = 3)]
    [Slider(Min = 1, Max = 8, Step = 1, Default = 4)]
    public int MaxParallelism { get; init; } = 4;
}
