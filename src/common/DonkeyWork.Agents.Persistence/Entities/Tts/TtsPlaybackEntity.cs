namespace DonkeyWork.Agents.Persistence.Entities.Tts;

/// <summary>
/// Tracks playback state for a TTS recording. Last write wins.
/// One record per user per recording.
/// </summary>
public class TtsPlaybackEntity : BaseEntity
{
    /// <summary>
    /// The recording this playback state belongs to.
    /// </summary>
    public Guid RecordingId { get; set; }

    /// <summary>
    /// Current playback position in seconds.
    /// </summary>
    public double PositionSeconds { get; set; }

    /// <summary>
    /// Total duration of the audio in seconds (reported by the player).
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Whether the user has completed playback at least once.
    /// </summary>
    public bool Completed { get; set; }

    /// <summary>
    /// Playback speed the user last used (e.g., 1.0, 1.5, 2.0).
    /// </summary>
    public double PlaybackSpeed { get; set; } = 1.0;

    /// <summary>
    /// Navigation property to the parent recording.
    /// </summary>
    public TtsRecordingEntity Recording { get; set; } = null!;
}
