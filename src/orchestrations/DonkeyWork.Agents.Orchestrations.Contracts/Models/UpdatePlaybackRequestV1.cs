namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

public sealed class UpdatePlaybackRequestV1
{
    public required double PositionSeconds { get; init; }
    public required double DurationSeconds { get; init; }
    public bool Completed { get; init; }
    public double PlaybackSpeed { get; init; } = 1.0;
}
