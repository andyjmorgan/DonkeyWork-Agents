namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class GetPublicUrlResponseV1
{
    public required string Url { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
