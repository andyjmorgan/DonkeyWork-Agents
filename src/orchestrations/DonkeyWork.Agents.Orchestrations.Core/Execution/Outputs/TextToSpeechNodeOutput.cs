namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// Output from a TextToSpeech node — a single audio asset.
/// The node's job is to chunk input to fit the model's per-call limit, generate
/// audio for each chunk in parallel, and stitch the results into one final blob.
/// Consumers always see one stream; chunking is an internal detail.
/// </summary>
public class TextToSpeechNodeOutput : NodeOutput
{
    /// <summary>
    /// Base64-encoded audio for the full stitched output.
    /// </summary>
    public required string AudioBase64 { get; init; }

    /// <summary>
    /// Content type of the audio (e.g. "audio/mpeg").
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// File extension matching the audio format (e.g. "mp3").
    /// </summary>
    public required string FileExtension { get; init; }

    /// <summary>
    /// Size of the stitched audio in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// The full transcript that was synthesized (joined across internal chunks).
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
        => $"Audio generated: {SizeBytes} bytes {FileExtension}, voice: {Voice}";

    /// <inheritdoc />
    /// <remarks>
    /// Strips raw base64 audio before serialization so trace events and
    /// per-step Input/Output records hold only metadata, not megabytes of audio.
    /// </remarks>
    public override object ToTraceOutput() => new
    {
        AudioBase64 = $"<audio:{SizeBytes} bytes>",
        ContentType,
        FileExtension,
        SizeBytes,
        Transcript,
        Voice,
        Model,
    };
}
