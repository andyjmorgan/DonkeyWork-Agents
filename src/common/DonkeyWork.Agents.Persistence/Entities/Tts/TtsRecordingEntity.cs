namespace DonkeyWork.Agents.Persistence.Entities.Tts;

/// <summary>
/// Represents a TTS audio recording with metadata.
/// </summary>
public class TtsRecordingEntity : BaseEntity
{
    /// <summary>
    /// Display name for the recording.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the recording content.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// S3 object key for the audio file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// The original text that was converted to speech.
    /// </summary>
    public string Transcript { get; set; } = string.Empty;

    /// <summary>
    /// Content type of the audio file (e.g., "audio/mpeg").
    /// </summary>
    public string ContentType { get; set; } = "audio/mpeg";

    /// <summary>
    /// Size of the audio file in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// The voice used for generation (e.g., "alloy", "nova").
    /// </summary>
    public string? Voice { get; set; }

    /// <summary>
    /// The TTS model used (e.g., "tts-1", "tts-1-hd").
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// The orchestration execution that created this recording, if any.
    /// </summary>
    public Guid? OrchestrationExecutionId { get; set; }

    /// <summary>
    /// The collection (folder) this recording belongs to, if any.
    /// </summary>
    public Guid? CollectionId { get; set; }

    /// <summary>
    /// Position of this recording within its collection.
    /// </summary>
    public int? SequenceNumber { get; set; }

    /// <summary>
    /// Chapter-style title within the collection; falls back to <see cref="Name"/> when null.
    /// </summary>
    public string? ChapterTitle { get; set; }

    /// <summary>
    /// Lifecycle state of the recording's audio generation.
    /// </summary>
    public TtsRecordingStatus Status { get; set; } = TtsRecordingStatus.Ready;

    /// <summary>
    /// Generation progress in [0.0, 1.0]. Always 1.0 once <see cref="Status"/> is Ready.
    /// </summary>
    public double Progress { get; set; } = 1.0;

    /// <summary>
    /// Error message when <see cref="Status"/> is Failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Collection this recording belongs to, if any.
    /// </summary>
    public TtsAudioCollectionEntity? Collection { get; set; }

    /// <summary>
    /// Playback state for this recording (one per user, last write wins).
    /// </summary>
    public TtsPlaybackEntity? Playback { get; set; }
}
