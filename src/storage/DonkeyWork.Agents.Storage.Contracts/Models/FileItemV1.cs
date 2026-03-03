namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class FileItemV1
{
    public required string FileName { get; init; }

    public required long SizeBytes { get; init; }

    public required DateTimeOffset LastModified { get; init; }
}
