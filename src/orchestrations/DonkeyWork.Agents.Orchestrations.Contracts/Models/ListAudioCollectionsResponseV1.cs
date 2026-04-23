namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

public sealed class ListAudioCollectionsResponseV1
{
    public required List<AudioCollectionV1> Items { get; init; }
    public required int TotalCount { get; init; }
}
