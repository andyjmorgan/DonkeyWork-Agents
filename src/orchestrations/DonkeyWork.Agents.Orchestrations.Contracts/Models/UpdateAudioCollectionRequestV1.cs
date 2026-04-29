namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

public sealed class UpdateAudioCollectionRequestV1
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? CoverImagePath { get; init; }
    public string? DefaultVoice { get; init; }
    public string? DefaultModel { get; init; }
}
