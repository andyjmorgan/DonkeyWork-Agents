namespace DonkeyWork.Agents.Persistence.Entities.Tts;

/// <summary>
/// A user-scoped folder that groups ordered TTS recordings as chapters.
/// </summary>
public class TtsAudioCollectionEntity : BaseEntity
{
    /// <summary>
    /// Display name for the collection.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the collection contains.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional S3 object key for a cover image.
    /// </summary>
    public string? CoverImagePath { get; set; }

    /// <summary>
    /// Default voice applied to new recordings created in this collection.
    /// </summary>
    public string? DefaultVoice { get; set; }

    /// <summary>
    /// Default TTS model applied to new recordings created in this collection.
    /// </summary>
    public string? DefaultModel { get; set; }

    /// <summary>
    /// Recordings that belong to this collection.
    /// </summary>
    public ICollection<TtsRecordingEntity> Recordings { get; set; } = new List<TtsRecordingEntity>();
}
