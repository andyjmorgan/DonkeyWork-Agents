using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the TextToSpeech node - generates speech audio from text.
/// Currently supports OpenAI TTS models; designed for future provider extensibility.
/// </summary>
[Node(
    DisplayName = "Text to Speech",
    Description = "Generate speech audio from text using OpenAI TTS",
    Category = "Audio",
    Icon = "volume-2",
    Color = "pink")]
public sealed class TextToSpeechNodeConfiguration : NodeConfiguration, IRequiresCredential
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.TextToSpeech;

    /// <summary>
    /// The credential ID for authenticating with the TTS provider.
    /// </summary>
    [JsonPropertyName("credentialId")]
    [ConfigurableField(Label = "Credential", ControlType = ControlType.Credential, Order = 10)]
    [Tab("Basic", Order = 1, Icon = "settings")]
    public required Guid CredentialId { get; init; }

    /// <summary>
    /// The TTS model to use.
    /// </summary>
    [JsonPropertyName("model")]
    [ConfigurableField(Label = "Model", ControlType = ControlType.Text, Order = 20)]
    [Tab("Basic", Order = 1)]
    [Immutable]
    public required string Model { get; init; }

    /// <summary>
    /// The voice to use for speech generation.
    /// All voices work with gpt-4o-mini-tts; tts-1/tts-1-hd support alloy, ash, coral, echo, fable, nova, onyx, sage, shimmer.
    /// </summary>
    [JsonPropertyName("voice")]
    [ConfigurableField(Label = "Voice", ControlType = ControlType.Select, Order = 30)]
    [Tab("Basic", Order = 1)]
    [SelectOptions("alloy", "ash", "ballad", "coral", "echo", "fable", "nova", "onyx", "sage", "shimmer", "verse", Default = "alloy")]
    public required string Voice { get; init; }

    /// <summary>
    /// The text(s) to convert to speech. Must render to a JSON array of strings — each
    /// element is generated as a separate clip and fan-out is bounded by <see cref="MaxParallelism"/>.
    /// For a single clip, wrap the input as a JSON array literal:
    ///   <c>["{{ Input.text | string.escape }}"]</c>
    /// For chunked input from a ChunkText node:
    ///   <c>{{ Steps.chunk_node.Chunks | to_json }}</c>
    /// </summary>
    [JsonPropertyName("inputs")]
    [ConfigurableField(Label = "Inputs (JSON array)", ControlType = ControlType.TextArea, Order = 10, Required = true,
        Description = "JSON array of text chunks. Use {{ Steps.chunk_node.Chunks | to_json }} or [\"your text\"] for a single clip.")]
    [Tab("Content", Order = 2, Icon = "file-text")]
    [SupportVariables]
    public required string Inputs { get; init; }

    /// <summary>
    /// Optional instructions for voice steering (tone, pacing, emotion).
    /// Only supported by gpt-4o-mini-tts.
    /// </summary>
    [JsonPropertyName("instructions")]
    [ConfigurableField(Label = "Voice Instructions", ControlType = ControlType.TextArea, Order = 20,
        Description = "Guide the voice style: tone, pacing, emotion. Only supported by gpt-4o-mini-tts.",
        Placeholder = "e.g. Speak in a warm, conversational tone with moderate pacing.")]
    [Tab("Content", Order = 2)]
    [SupportVariables]
    public string? Instructions { get; init; }

    /// <summary>
    /// The speed of the generated audio (0.25 to 4.0).
    /// </summary>
    [JsonPropertyName("speed")]
    [ConfigurableField(Label = "Speed", ControlType = ControlType.Slider, Order = 10)]
    [Tab("Advanced", Order = 3, Icon = "sliders")]
    [Slider(Min = 0.25, Max = 4.0, Step = 0.25, Default = 1.0)]
    public double Speed { get; init; } = 1.0;

    /// <summary>
    /// The audio output format.
    /// </summary>
    [JsonPropertyName("responseFormat")]
    [ConfigurableField(Label = "Format", ControlType = ControlType.Select, Order = 20)]
    [Tab("Advanced", Order = 3)]
    [SelectOptions("mp3", "opus", "aac", "flac", "wav", Default = "mp3")]
    public string ResponseFormat { get; init; } = "mp3";

    /// <summary>
    /// Maximum number of clips to generate in parallel. Bounded by provider rate limits.
    /// </summary>
    [JsonPropertyName("maxParallelism")]
    [ConfigurableField(Label = "Max Parallelism", ControlType = ControlType.Slider, Order = 30,
        Description = "Maximum concurrent provider calls when processing multiple chunks.")]
    [Tab("Advanced", Order = 3)]
    [Slider(Min = 1, Max = 16, Step = 1, Default = 4)]
    public int MaxParallelism { get; init; } = 4;
}
