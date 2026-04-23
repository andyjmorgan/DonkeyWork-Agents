namespace DonkeyWork.Agents.Persistence.Entities.Tts;

/// <summary>
/// Lifecycle state of a TTS recording's audio generation.
/// </summary>
public enum TtsRecordingStatus
{
    Pending,
    Generating,
    Ready,
    Failed,
}
