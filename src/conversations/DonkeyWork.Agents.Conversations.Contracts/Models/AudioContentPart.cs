using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Audio content part. References a TTS recording stored in the system.
/// </summary>
public sealed class AudioContentPart : ContentPart
{
    /// <summary>
    /// The ID of the TTS recording.
    /// </summary>
    [JsonPropertyName("recordingId")]
    public required string RecordingId { get; set; }

    /// <summary>
    /// The object key of the audio file in blob storage.
    /// </summary>
    [JsonPropertyName("objectKey")]
    public required string ObjectKey { get; set; }

    /// <summary>
    /// The media type of the audio (e.g., "audio/mpeg").
    /// </summary>
    [JsonPropertyName("mediaType")]
    public required string MediaType { get; set; }

    /// <summary>
    /// The transcript of the audio content.
    /// </summary>
    [JsonPropertyName("transcript")]
    public string? Transcript { get; set; }

    /// <summary>
    /// Display name for the recording.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
