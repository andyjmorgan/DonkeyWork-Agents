using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the Gemini Text-to-Speech node. Long text is chunked internally
/// on natural boundaries (paragraphs, list items, sentences) and clips are stitched
/// downstream.
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
    /// Plain-text or markdown-ish input. Markdown formatting is stripped before synthesis.
    /// Long inputs are chunked on natural boundaries and rendered as parallel clips.
    /// </summary>
    [JsonPropertyName("text")]
    [ConfigurableField(Label = "Text", ControlType = ControlType.TextArea, Order = 10, Required = true,
        Description = "Text to speak. Markdown formatting is stripped. Long input is chunked automatically.")]
    [Tab("Content", Order = 2, Icon = "file-text")]
    [SupportVariables]
    public required string Text { get; init; }

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
    /// Target characters per chunk. Chunks are packed greedily up to this size.
    /// </summary>
    [JsonPropertyName("targetCharCount")]
    [ConfigurableField(Label = "Target Chars", ControlType = ControlType.Slider, Order = 20,
        Description = "Target characters per chunk. Chunker packs blocks up to this size.")]
    [Tab("Advanced", Order = 3)]
    [Slider(Min = 500, Max = 7500, Step = 250, Default = 4000)]
    public int TargetCharCount { get; init; } = 4000;

    /// <summary>
    /// Hard ceiling per chunk. Gemini TTS comfortable around 8K chars.
    /// </summary>
    [JsonPropertyName("maxCharCount")]
    [ConfigurableField(Label = "Max Chars", ControlType = ControlType.Slider, Order = 30,
        Description = "Hard ceiling per chunk. Gemini TTS comfortable up to ~8000 chars.")]
    [Tab("Advanced", Order = 3)]
    [Slider(Min = 500, Max = 8000, Step = 250, Default = 7500)]
    public int MaxCharCount { get; init; } = 7500;

    /// <summary>
    /// Maximum concurrent provider calls when synthesizing multiple chunks.
    /// </summary>
    [JsonPropertyName("maxParallelism")]
    [ConfigurableField(Label = "Max Parallelism", ControlType = ControlType.Slider, Order = 40,
        Description = "Maximum concurrent provider calls when synthesizing multiple chunks.")]
    [Tab("Advanced", Order = 3)]
    [Slider(Min = 1, Max = 8, Step = 1, Default = 4)]
    public int MaxParallelism { get; init; } = 4;
}
