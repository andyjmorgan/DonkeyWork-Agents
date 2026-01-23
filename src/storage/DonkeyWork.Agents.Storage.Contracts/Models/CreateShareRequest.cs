namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class CreateShareRequest
{
    public required Guid FileId { get; init; }

    public TimeSpan? ExpiresIn { get; init; }

    public int? MaxDownloads { get; init; }

    public string? Password { get; init; }
}
