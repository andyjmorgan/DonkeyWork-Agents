using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class StoredFile
{
    public required Guid Id { get; init; }

    public required Guid UserId { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long SizeBytes { get; init; }

    public required string BucketName { get; init; }

    public required string ObjectKey { get; init; }

    public string? ChecksumSha256 { get; init; }

    public required FileStatus Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? MarkedForDeletionAt { get; init; }

    public Dictionary<string, string>? Metadata { get; init; }
}
