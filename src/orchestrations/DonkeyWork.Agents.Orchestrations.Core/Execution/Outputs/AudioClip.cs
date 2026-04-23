namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// One piece of generated audio — a single TTS provider call's output.
/// A recording may be stitched from many clips by <c>ConcatAudio</c>.
/// </summary>
public sealed class AudioClip
{
    /// <summary>
    /// The audio data encoded as a base64 string.
    /// </summary>
    public required string AudioBase64 { get; init; }

    /// <summary>
    /// The content type of the audio (e.g., "audio/mpeg").
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// The file extension (e.g., "mp3").
    /// </summary>
    public required string FileExtension { get; init; }

    /// <summary>
    /// The size of the audio in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// The text that was converted to speech for this clip.
    /// </summary>
    public required string Transcript { get; init; }
}
