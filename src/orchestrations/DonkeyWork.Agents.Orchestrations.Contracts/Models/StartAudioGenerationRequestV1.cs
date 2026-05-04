namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

public sealed class StartAudioGenerationRequestV1
{
    public required string Text { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Model { get; init; }
    public required string Voice { get; init; }
    public string? Instructions { get; init; }
    public Guid? CollectionId { get; init; }
    public int? SequenceNumber { get; init; }
    public string? ChapterTitle { get; init; }
    public int TargetCharCount { get; init; } = 1500;
    public int MaxCharCount { get; init; } = 2500;
    public int MaxParallelism { get; init; } = 4;
    public string ResponseFormat { get; init; } = "mp3";
    public double Speed { get; init; } = 1.0;
}
