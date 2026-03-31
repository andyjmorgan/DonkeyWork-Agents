using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the StoreAudio node - stores generated audio with metadata as a TTS recording.
/// This is a convergence node that combines outputs from a TTS node and a metadata model node.
/// </summary>
[Node(
    DisplayName = "Store Audio",
    Description = "Save generated audio with metadata as a recording",
    Category = "Utility",
    Icon = "database",
    Color = "emerald")]
public sealed class StoreAudioNodeConfiguration : NodeConfiguration
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.StoreAudio;

    /// <summary>
    /// Template expression for the recording name.
    /// </summary>
    [JsonPropertyName("recordingName")]
    [ConfigurableField(Label = "Recording Name", ControlType = ControlType.TextArea, Order = 10, Required = true,
        Description = "Name for the recording. Use {{Steps.node_name.Property}} or {{Steps.node_name.ResponseText | json_value \"name\"}} for model output.")]
    [Tab("Mapping", Order = 1, Icon = "link")]
    [SupportVariables]
    public required string RecordingName { get; init; }

    /// <summary>
    /// Template expression for the recording description.
    /// </summary>
    [JsonPropertyName("recordingDescription")]
    [ConfigurableField(Label = "Recording Description", ControlType = ControlType.TextArea, Order = 20, Required = true,
        Description = "Description for the recording. Use template variables to reference model output.")]
    [Tab("Mapping", Order = 1)]
    [SupportVariables]
    public required string RecordingDescription { get; init; }

    /// <summary>
    /// Template expression for the audio object key from the TTS node output.
    /// </summary>
    [JsonPropertyName("audioObjectKey")]
    [ConfigurableField(Label = "Audio Object Key", ControlType = ControlType.Text, Order = 30, Required = true,
        Description = "The S3 object key of the audio file. Use {{Steps.tts_node.ObjectKey}}.")]
    [Tab("Mapping", Order = 1)]
    [SupportVariables]
    public required string AudioObjectKey { get; init; }

    /// <summary>
    /// Template expression for the transcript text.
    /// </summary>
    [JsonPropertyName("transcript")]
    [ConfigurableField(Label = "Transcript", ControlType = ControlType.TextArea, Order = 40, Required = true,
        Description = "The transcript text. Use {{Steps.tts_node.Transcript}} or {{Input.text}}.")]
    [Tab("Mapping", Order = 1)]
    [SupportVariables]
    public required string Transcript { get; init; }

    /// <summary>
    /// Template expression for the audio content type.
    /// </summary>
    [JsonPropertyName("audioContentType")]
    [ConfigurableField(Label = "Audio Content Type", ControlType = ControlType.Text, Order = 50,
        Description = "Content type of the audio file. Use {{Steps.tts_node.ContentType}}.")]
    [Tab("Mapping", Order = 1)]
    [SupportVariables]
    public string AudioContentType { get; init; } = "audio/mpeg";

    /// <summary>
    /// Template expression for the audio file size in bytes.
    /// </summary>
    [JsonPropertyName("audioSizeBytes")]
    [ConfigurableField(Label = "Audio Size (bytes)", ControlType = ControlType.Text, Order = 60,
        Description = "Size of the audio file. Use {{Steps.tts_node.SizeBytes}}.")]
    [Tab("Mapping", Order = 1)]
    [SupportVariables]
    public string AudioSizeBytes { get; init; } = "0";

    /// <summary>
    /// Template expression for the voice used for generation.
    /// </summary>
    [JsonPropertyName("voice")]
    [ConfigurableField(Label = "Voice", ControlType = ControlType.Text, Order = 70,
        Description = "The voice used for generation. Use {{Steps.tts_node.Voice}}.")]
    [Tab("Mapping", Order = 1)]
    [SupportVariables]
    public string? Voice { get; init; }

    /// <summary>
    /// Template expression for the TTS model used.
    /// </summary>
    [JsonPropertyName("model")]
    [ConfigurableField(Label = "Model", ControlType = ControlType.Text, Order = 80,
        Description = "The TTS model used. Use {{Steps.tts_node.Model}}.")]
    [Tab("Mapping", Order = 1)]
    [SupportVariables]
    public string? Model { get; init; }
}
