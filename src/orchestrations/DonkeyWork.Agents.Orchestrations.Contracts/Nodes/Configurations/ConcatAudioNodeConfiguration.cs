using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the ConcatAudio node — stitches a sequence of same-format
/// audio clips (typically a TextToSpeech output) into a single file via ffmpeg
/// stream-copy (no re-encoding).
/// </summary>
[Node(
    DisplayName = "Concat Audio",
    Description = "Stitch multiple audio clips into a single file",
    Category = "Audio",
    Icon = "layers",
    Color = "pink")]
public sealed class ConcatAudioNodeConfiguration : NodeConfiguration
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.ConcatAudio;

    /// <summary>
    /// Reference to the upstream TextToSpeech node whose clips should be stitched.
    /// The template must evaluate to a TextToSpeech output (i.e. carry a <c>Clips</c> array).
    /// Use: <c>{{ Steps.tts_node }}</c>
    /// </summary>
    [JsonPropertyName("sourceNode")]
    [ConfigurableField(Label = "Source Node", ControlType = ControlType.Text, Order = 10, Required = true,
        Description = "The upstream TTS node whose Clips should be concatenated. e.g. tts_node")]
    [Tab("Settings", Order = 1, Icon = "settings")]
    [SupportVariables]
    public required string SourceNode { get; init; }

    /// <summary>
    /// Output format — must match the upstream clips' format (stream-copy concat).
    /// </summary>
    [JsonPropertyName("format")]
    [ConfigurableField(Label = "Format", ControlType = ControlType.Select, Order = 20,
        Description = "Must match the format of the upstream TTS clips.")]
    [Tab("Settings", Order = 1)]
    [SelectOptions("mp3", "wav", "aac", "flac", "opus", Default = "mp3")]
    public string Format { get; init; } = "mp3";
}
