namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class S3ObjectMetadata
{
    public required string ContentType { get; init; }

    public required long SizeBytes { get; init; }

    public required DateTimeOffset LastModified { get; init; }
}
