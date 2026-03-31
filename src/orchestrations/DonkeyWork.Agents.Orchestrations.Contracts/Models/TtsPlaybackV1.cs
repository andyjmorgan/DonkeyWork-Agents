namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

public sealed class TtsPlaybackV1
{
    public required double PositionSeconds { get; init; }
    public required double DurationSeconds { get; init; }
    public required bool Completed { get; init; }
    public required double PlaybackSpeed { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
