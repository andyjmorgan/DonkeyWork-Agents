namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// Output from a StoreAudio node execution.
/// </summary>
public class StoreAudioNodeOutput : NodeOutput
{
    /// <summary>
    /// The ID of the created TTS recording entity.
    /// </summary>
    public required Guid RecordingId { get; init; }

    /// <summary>
    /// The name of the recording.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The description of the recording.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The S3 object key of the audio file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The transcript of the recording.
    /// </summary>
    public required string Transcript { get; init; }

    public override string ToMessageOutput()
    {
        return $"Recording saved: {Name} (ID: {RecordingId})";
    }
}
