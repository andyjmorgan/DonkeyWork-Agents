namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// Output from a TextToSpeech node execution.
/// </summary>
public class TextToSpeechNodeOutput : NodeOutput
{
    /// <summary>
    /// The S3 object key of the uploaded audio file.
    /// </summary>
    public required string ObjectKey { get; init; }

    /// <summary>
    /// The file name of the audio file.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// The content type of the audio file (e.g., "audio/mpeg").
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// The size of the audio file in bytes.
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
        return $"Audio generated: {FileName} ({SizeBytes} bytes, voice: {Voice})";
    }
}
