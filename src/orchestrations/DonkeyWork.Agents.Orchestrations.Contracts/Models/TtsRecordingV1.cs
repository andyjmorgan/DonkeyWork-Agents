namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

public sealed class TtsRecordingV1
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string FilePath { get; init; }
    public required string Transcript { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public string? Voice { get; init; }
    public string? Model { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public TtsPlaybackV1? Playback { get; init; }
}
