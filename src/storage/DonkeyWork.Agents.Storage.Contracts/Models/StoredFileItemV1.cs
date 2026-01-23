using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class StoredFileItemV1
{
    public required Guid Id { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long SizeBytes { get; init; }

    public required FileStatus Status { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
