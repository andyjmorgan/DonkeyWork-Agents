using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class FileShare
{
    public required Guid Id { get; init; }

    public required Guid FileId { get; init; }

    public required Guid UserId { get; init; }

    public required string ShareToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required ShareStatus Status { get; init; }

    public int? MaxDownloads { get; init; }

    public required int DownloadCount { get; init; }

    public required bool HasPassword { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
