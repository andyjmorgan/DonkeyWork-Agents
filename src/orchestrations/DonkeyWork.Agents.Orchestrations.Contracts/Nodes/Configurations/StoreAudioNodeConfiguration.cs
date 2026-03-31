using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the StoreAudio node - stores generated audio with metadata as a TTS recording.
/// Audio data fields auto-resolve from the upstream TextToSpeech node output when left empty.
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
    /// Override for the audio object key. Auto-resolves from upstream TTS node when empty.
    /// </summary>
    [JsonPropertyName("audioObjectKey")]
    [ConfigurableField(Label = "Audio Object Key", ControlType = ControlType.Text, Order = 10,
        Placeholder = "Auto-resolved from TTS node",
        Description = "Leave empty to auto-resolve from the upstream TTS node output.")]
    [Tab("Advanced", Order = 2, Icon = "sliders")]
    [SupportVariables]
    public string? AudioObjectKey { get; init; }

    /// <summary>
    /// Override for the transcript. Auto-resolves from upstream TTS node when empty.
    /// </summary>
    [JsonPropertyName("transcript")]
    [ConfigurableField(Label = "Transcript", ControlType = ControlType.TextArea, Order = 20,
        Placeholder = "Auto-resolved from TTS node",
        Description = "Leave empty to auto-resolve from the upstream TTS node output.")]
    [Tab("Advanced", Order = 2)]
    [SupportVariables]
    public string? Transcript { get; init; }

    /// <summary>
    /// Override for the audio content type. Auto-resolves from upstream TTS node when empty.
    /// </summary>
    [JsonPropertyName("audioContentType")]
    [ConfigurableField(Label = "Audio Content Type", ControlType = ControlType.Text, Order = 30,
        Placeholder = "Auto-resolved from TTS node",
        Description = "Leave empty to auto-resolve from the upstream TTS node output.")]
    [Tab("Advanced", Order = 2)]
    [SupportVariables]
    public string? AudioContentType { get; init; }

    /// <summary>
    /// Override for the voice metadata. Auto-resolves from upstream TTS node when empty.
    /// </summary>
    [JsonPropertyName("voice")]
    [ConfigurableField(Label = "Voice", ControlType = ControlType.Text, Order = 40,
        Placeholder = "Auto-resolved from TTS node",
        Description = "Leave empty to auto-resolve from the upstream TTS node output.")]
    [Tab("Advanced", Order = 2)]
    [SupportVariables]
    public string? Voice { get; init; }

    /// <summary>
    /// Override for the model metadata. Auto-resolves from upstream TTS node when empty.
    /// </summary>
    [JsonPropertyName("model")]
    [ConfigurableField(Label = "Model", ControlType = ControlType.Text, Order = 50,
        Placeholder = "Auto-resolved from TTS node",
        Description = "Leave empty to auto-resolve from the upstream TTS node output.")]
    [Tab("Advanced", Order = 2)]
    [SupportVariables]
    public string? Model { get; init; }
}
