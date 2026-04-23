namespace DonkeyWork.Agents.Orchestrations.Contracts.Messages;

/// <summary>
/// Wolverine command dispatched when an agent-initiated audio generation starts.
/// The handler performs the chunk → TTS → concat → store pipeline asynchronously
/// and updates the recording's Status/Progress as it runs.
/// </summary>
public sealed record GenerateAudioRecordingCommand(
    Guid RecordingId,
    Guid UserId,
    string Text,
    string Model,
    string Voice,
    string? Instructions,
    int TargetCharCount,
    int MaxCharCount,
    int MaxParallelism,
    string ResponseFormat,
    double Speed);
