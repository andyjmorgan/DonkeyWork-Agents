namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

public sealed class GetAudioUrlResponseV1
{
    public required string Url { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string ContentType { get; init; }
}
