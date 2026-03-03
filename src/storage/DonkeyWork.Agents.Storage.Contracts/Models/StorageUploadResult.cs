namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class StorageUploadResult
{
    public required string ObjectKey { get; init; }

    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required long SizeBytes { get; init; }
}
