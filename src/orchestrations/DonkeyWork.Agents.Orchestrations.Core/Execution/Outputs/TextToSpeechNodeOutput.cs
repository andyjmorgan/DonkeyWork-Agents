namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// Output from a TextToSpeech node execution.
/// Contains the generated audio as base64 and metadata. Storage is handled downstream by StoreAudio.
/// </summary>
public class TextToSpeechNodeOutput : NodeOutput
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
    /// The original text that was converted to speech.
    /// </summary>
    public required string Transcript { get; init; }

    /// <summary>
    /// The voice used for generation.
    /// </summary>
    public required string Voice { get; init; }

    /// <summary>
    /// The model used for generation.
    /// </summary>
    public required string Model { get; init; }

    public override string ToMessageOutput()
    {
        return $"Audio generated: {SizeBytes} bytes, voice: {Voice}, format: {FileExtension}";
    }
}
