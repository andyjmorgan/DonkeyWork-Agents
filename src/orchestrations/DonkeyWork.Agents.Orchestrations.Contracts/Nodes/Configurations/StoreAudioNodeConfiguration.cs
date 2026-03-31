using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the StoreAudio node - stores audio with metadata as a TTS recording.
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
        Description = "Name for the recording. Use {{Steps.node_name.ResponseText | json_value \"name\"}} for model output.")]
    [Tab("Settings", Order = 1, Icon = "settings")]
    [SupportVariables]
    public required string RecordingName { get; init; }

    /// <summary>
    /// Template expression for the recording description.
    /// </summary>
    [JsonPropertyName("recordingDescription")]
    [ConfigurableField(Label = "Recording Description", ControlType = ControlType.TextArea, Order = 20, Required = true,
        Description = "Description for the recording. Use template variables to reference model output.")]
    [Tab("Settings", Order = 1)]
    [SupportVariables]
    public required string RecordingDescription { get; init; }

    /// <summary>
    /// Base64-encoded audio data. Use {{Steps.tts_node.AudioBase64}}.
    /// </summary>
    [JsonPropertyName("audioBase64")]
    [ConfigurableField(Label = "Audio Data (Base64)", ControlType = ControlType.Text, Order = 30, Required = true,
        Description = "Base64 audio data. Use {{Steps.tts_node.AudioBase64}}.")]
    [Tab("Audio", Order = 2, Icon = "volume-2")]
    [SupportVariables]
    public required string AudioBase64 { get; init; }

    /// <summary>
    /// Content type of the audio. Use {{Steps.tts_node.ContentType}}.
    /// </summary>
    [JsonPropertyName("contentType")]
    [ConfigurableField(Label = "Content Type", ControlType = ControlType.Text, Order = 40, Required = true,
        Description = "MIME type of the audio. Use {{Steps.tts_node.ContentType}}.")]
    [Tab("Audio", Order = 2)]
    [SupportVariables]
    public required string ContentType { get; init; }
}
