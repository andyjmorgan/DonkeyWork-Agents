using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the TextToSpeech node — generates speech audio from text using
/// OpenAI TTS. Long text is chunked internally on natural boundaries (paragraphs,
/// list items, sentences) and clips are stitched downstream.
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

    [JsonPropertyName("credentialId")]
    [ConfigurableField(Label = "Credential", ControlType = ControlType.Credential, Order = 10)]
    [Tab("Basic", Order = 1, Icon = "settings")]
    public required Guid CredentialId { get; init; }

    [JsonPropertyName("model")]
    [ConfigurableField(Label = "Model", ControlType = ControlType.Text, Order = 20)]
    [Tab("Basic", Order = 1)]
    [Immutable]
    public required string Model { get; init; }

    /// <summary>
    /// All voices work with gpt-4o-mini-tts; tts-1/tts-1-hd support alloy, ash, coral,
    /// echo, fable, nova, onyx, sage, shimmer.
    /// </summary>
    [JsonPropertyName("voice")]
    [ConfigurableField(Label = "Voice", ControlType = ControlType.Select, Order = 30)]
    [Tab("Basic", Order = 1)]
    [SelectOptions("alloy", "ash", "ballad", "coral", "echo", "fable", "nova", "onyx", "sage", "shimmer", "verse", Default = "alloy")]
    public required string Voice { get; init; }

    /// <summary>
    /// Plain-text or markdown-ish input. Markdown formatting (bold, headings, links,
    /// code) is stripped before synthesis. Long inputs are chunked on natural
    /// boundaries and rendered as parallel clips.
    /// </summary>
    [JsonPropertyName("text")]
    [ConfigurableField(Label = "Text", ControlType = ControlType.TextArea, Order = 10, Required = true,
        Description = "Text to speak. Markdown formatting is stripped. Long input is chunked automatically.")]
    [Tab("Content", Order = 2, Icon = "file-text")]
    [SupportVariables]
    public required string Text { get; init; }

    /// <summary>
    /// Optional voice steering (tone, pacing, emotion). Only honoured by gpt-4o-mini-tts.
    /// </summary>
    [JsonPropertyName("instructions")]
    [ConfigurableField(Label = "Voice Instructions", ControlType = ControlType.TextArea, Order = 20,
        Description = "Guide the voice style: tone, pacing, emotion. Only supported by gpt-4o-mini-tts.",
        Placeholder = "e.g. Speak in a warm, conversational tone with moderate pacing.")]
    [Tab("Content", Order = 2)]
    [SupportVariables]
    public string? Instructions { get; init; }

    [JsonPropertyName("speed")]
    [ConfigurableField(Label = "Speed", ControlType = ControlType.Slider, Order = 10)]
    [Tab("Advanced", Order = 3, Icon = "sliders")]
    [Slider(Min = 0.25, Max = 4.0, Step = 0.25, Default = 1.0)]
    public double Speed { get; init; } = 1.0;

    [JsonPropertyName("responseFormat")]
    [ConfigurableField(Label = "Format", ControlType = ControlType.Select, Order = 20)]
    [Tab("Advanced", Order = 3)]
    [SelectOptions("mp3", "opus", "aac", "flac", "wav", Default = "mp3")]
    public string ResponseFormat { get; init; } = "mp3";

    /// <summary>
    /// Target characters per chunk. Chunks are packed greedily up to this size.
    /// </summary>
    [JsonPropertyName("targetCharCount")]
    [ConfigurableField(Label = "Target Chars", ControlType = ControlType.Slider, Order = 30,
        Description = "Target characters per chunk. Chunker packs blocks up to this size.")]
    [Tab("Advanced", Order = 3)]
    [Slider(Min = 500, Max = 3500, Step = 250, Default = 2000)]
    public int TargetCharCount { get; init; } = 2000;

    /// <summary>
    /// Hard ceiling per chunk. OpenAI TTS rejects requests over 4096 characters.
    /// </summary>
    [JsonPropertyName("maxCharCount")]
    [ConfigurableField(Label = "Max Chars", ControlType = ControlType.Slider, Order = 40,
        Description = "Hard ceiling per chunk (OpenAI rejects > 4096 chars).")]
    [Tab("Advanced", Order = 3)]
    [Slider(Min = 500, Max = 4000, Step = 250, Default = 3800)]
    public int MaxCharCount { get; init; } = 3800;

    /// <summary>
    /// Maximum concurrent provider calls when synthesizing multiple chunks.
    /// </summary>
    [JsonPropertyName("maxParallelism")]
    [ConfigurableField(Label = "Max Parallelism", ControlType = ControlType.Slider, Order = 50,
        Description = "Maximum concurrent provider calls when synthesizing multiple chunks.")]
    [Tab("Advanced", Order = 3)]
    [Slider(Min = 1, Max = 16, Step = 1, Default = 4)]
    public int MaxParallelism { get; init; } = 4;
}
