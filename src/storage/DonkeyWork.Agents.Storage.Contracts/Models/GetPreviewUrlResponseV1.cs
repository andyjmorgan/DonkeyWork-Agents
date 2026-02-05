namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class GetPreviewUrlResponseV1
{
    public required string Url { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }
}
