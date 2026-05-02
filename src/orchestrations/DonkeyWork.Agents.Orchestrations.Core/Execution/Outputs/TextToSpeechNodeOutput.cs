using System.Text.Json.Serialization;

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

    /// <summary>
    /// First clip's base64 audio. Convenience proxy for the common single-clip case
    /// so templates can write <c>{{ Steps.tts.AudioBase64 }}</c> instead of
    /// <c>{{ Steps.tts.Clips[0].AudioBase64 }}</c>. For chunked pipelines, route
    /// <c>Clips</c> through <c>ConcatAudio</c> and read the stitched result there.
    /// </summary>
    /// <remarks>
    /// <see cref="JsonIgnoreAttribute"/> keeps this out of serialized payloads
    /// (orchestration trace events, per-step DB records). Scriban templating
    /// reads it via reflection, so the convenience for templates is preserved.
    /// </remarks>
    [JsonIgnore]
    public string AudioBase64 => Clips.Count > 0 ? Clips[0].AudioBase64 : string.Empty;

    /// <summary>
    /// First clip's content type. Convenience proxy; see <see cref="AudioBase64"/>.
    /// </summary>
    [JsonIgnore]
    public string ContentType => Clips.Count > 0 ? Clips[0].ContentType : string.Empty;

    /// <summary>
    /// First clip's file extension. Convenience proxy; see <see cref="AudioBase64"/>.
    /// </summary>
    [JsonIgnore]
    public string FileExtension => Clips.Count > 0 ? Clips[0].FileExtension : string.Empty;

    /// <summary>
    /// First clip's size in bytes. Convenience proxy; see <see cref="AudioBase64"/>.
    /// </summary>
    [JsonIgnore]
    public long SizeBytes => Clips.Count > 0 ? Clips[0].SizeBytes : 0;

    /// <summary>
    /// First clip's transcript text. Convenience proxy; see <see cref="AudioBase64"/>.
    /// </summary>
    [JsonIgnore]
    public string Transcript => Clips.Count > 0 ? Clips[0].Transcript : string.Empty;

    public override string ToMessageOutput()
    {
        return $"Audio generated: {ClipCount} clip(s), {TotalSizeBytes} bytes total, voice: {Voice}";
    }

    /// <inheritdoc />
    /// <remarks>
    /// Strips raw base64 audio out of every clip before serialization so that
    /// trace events and persisted Input/Output records hold only metadata.
    /// </remarks>
    public override object ToTraceOutput()
    {
        return new
        {
            Clips = Clips.Select(c => new
            {
                AudioBase64 = $"<audio:{c.SizeBytes} bytes>",
                c.ContentType,
                c.FileExtension,
                c.SizeBytes,
                c.Transcript,
            }).ToList(),
            Voice,
            Model,
            TotalSizeBytes,
            ClipCount,
        };
    }
}
