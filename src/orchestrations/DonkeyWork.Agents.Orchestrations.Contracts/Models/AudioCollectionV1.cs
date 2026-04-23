namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

public sealed class AudioCollectionV1
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? CoverImagePath { get; init; }
    public string? DefaultVoice { get; init; }
    public string? DefaultModel { get; init; }
    public required int RecordingCount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
