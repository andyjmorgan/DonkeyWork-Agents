namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// Output from a TextToSpeech node — one or more generated audio clips.
/// Even a single-chunk generation returns a one-element <see cref="Clips"/> array.
/// Downstream nodes (typically <c>ConcatAudio</c>) combine clips into a stored recording.
/// </summary>
public class TextToSpeechNodeOutput : NodeOutput
{
    /// <summary>
    /// Generated audio clips, in the same order as the input chunks.
    /// </summary>
    public required IReadOnlyList<AudioClip> Clips { get; init; }

    /// <summary>
    /// The voice used for generation.
    /// </summary>
    public required string Voice { get; init; }

    /// <summary>
    /// The model used for generation.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Total size across all clips in bytes.
    /// </summary>
    public long TotalSizeBytes => Clips.Sum(c => c.SizeBytes);

    /// <summary>
    /// Number of clips generated.
    /// </summary>
    public int ClipCount => Clips.Count;

    public override string ToMessageOutput()
    {
        return $"Audio generated: {ClipCount} clip(s), {TotalSizeBytes} bytes total, voice: {Voice}";
    }
}
