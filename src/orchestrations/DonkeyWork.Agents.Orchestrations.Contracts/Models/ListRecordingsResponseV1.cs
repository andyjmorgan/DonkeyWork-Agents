namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

public sealed class ListRecordingsResponseV1
{
    public required List<TtsRecordingV1> Items { get; init; }
    public required int TotalCount { get; init; }
}
