namespace DonkeyWork.Agents.Storage.Contracts.Models;

public class PresignedUrlResult
{
    public required string Url { get; set; }

    public required DateTimeOffset ExpiresAt { get; set; }
}
