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
    [Tab("Settings", Order = 1)]
    [SupportVariables]
    public required string AudioBase64 { get; init; }

    /// <summary>
    /// Content type of the audio. Use {{Steps.tts_node.ContentType}}.
    /// </summary>
    [JsonPropertyName("contentType")]
    [ConfigurableField(Label = "Content Type", ControlType = ControlType.Text, Order = 40, Required = true,
        Description = "MIME type of the audio. Use {{Steps.tts_node.ContentType}}.")]
    [Tab("Settings", Order = 1)]
    [SupportVariables]
    public required string ContentType { get; init; }

    /// <summary>
    /// The voice used for generation. Use {{Steps.tts_node.Voice}}.
    /// </summary>
    [JsonPropertyName("voice")]
    [ConfigurableField(Label = "Voice", ControlType = ControlType.Text, Order = 50,
        Description = "The voice used. Use {{Steps.tts_node.Voice}}.")]
    [Tab("Settings", Order = 1)]
    [SupportVariables]
    public string? Voice { get; init; }

    /// <summary>
    /// The model used for generation. Use {{Steps.tts_node.Model}}.
    /// </summary>
    [JsonPropertyName("model")]
    [ConfigurableField(Label = "Model", ControlType = ControlType.Text, Order = 60,
        Description = "The model used. Use {{Steps.tts_node.Model}}.")]
    [Tab("Settings", Order = 1)]
    [SupportVariables]
    public string? Model { get; init; }

    /// <summary>
    /// The transcript text. Use {{Steps.tts_node.Transcript}}.
    /// </summary>
    [JsonPropertyName("transcript")]
    [ConfigurableField(Label = "Transcript", ControlType = ControlType.TextArea, Order = 70,
        Description = "The transcript text. Use {{Steps.tts_node.Transcript}}.")]
    [Tab("Settings", Order = 1)]
    [SupportVariables]
    public string? Transcript { get; init; }

    /// <summary>
    /// The file extension. Use {{Steps.tts_node.FileExtension}}.
    /// </summary>
    [JsonPropertyName("fileExtension")]
    [ConfigurableField(Label = "File Extension", ControlType = ControlType.Text, Order = 80,
        Description = "File extension (e.g. mp3). Use {{Steps.tts_node.FileExtension}}.")]
    [Tab("Settings", Order = 1)]
    [SupportVariables]
    public string? FileExtension { get; init; }

    /// <summary>
    /// Optional collection (folder) to drop the recording into. Use a UUID literal
    /// or a template that renders to a UUID. Leave blank to store unfiled.
    /// </summary>
    [JsonPropertyName("collectionId")]
    [ConfigurableField(Label = "Collection ID", ControlType = ControlType.Text, Order = 10,
        Description = "UUID of an audio collection. Leave blank to store unfiled.")]
    [Tab("Collection", Order = 2, Icon = "folder")]
    [SupportVariables]
    public string? CollectionId { get; init; }

    /// <summary>
    /// Optional sequence number within the collection for chapter-style ordering.
    /// </summary>
    [JsonPropertyName("sequenceNumber")]
    [ConfigurableField(Label = "Sequence Number", ControlType = ControlType.Text, Order = 20,
        Description = "Position within the collection. Leave blank to append at the end.")]
    [Tab("Collection", Order = 2)]
    [SupportVariables]
    public string? SequenceNumber { get; init; }

    /// <summary>
    /// Optional chapter-style title; falls back to RecordingName in the UI when null.
    /// </summary>
    [JsonPropertyName("chapterTitle")]
    [ConfigurableField(Label = "Chapter Title", ControlType = ControlType.Text, Order = 30,
        Description = "Optional chapter title within the collection.")]
    [Tab("Collection", Order = 2)]
    [SupportVariables]
    public string? ChapterTitle { get; init; }
}
