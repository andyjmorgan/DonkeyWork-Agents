namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// Output from a ConcatAudio node — a single stitched audio file produced
/// from an ordered list of clips. Shape matches a single AudioClip so it
/// can be consumed by StoreAudio via template variables.
/// </summary>
public class ConcatAudioNodeOutput : NodeOutput
{
    /// <summary>
    /// The stitched audio data, base64-encoded.
    /// </summary>
    public required string AudioBase64 { get; init; }

    /// <summary>
    /// Content type of the stitched audio.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// File extension of the stitched audio.
    /// </summary>
    public required string FileExtension { get; init; }

    /// <summary>
    /// Size of the stitched audio in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Transcript of the full stitched recording (joined from clips with a single space).
    /// </summary>
    public required string Transcript { get; init; }

    /// <summary>
    /// Number of clips that were stitched together.
    /// </summary>
    public required int ClipCount { get; init; }

    public override string ToMessageOutput()
    {
        return $"Stitched {ClipCount} clips → {SizeBytes} bytes {FileExtension}";
    }
}
