namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class WriteFileResponseV1
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public required long Size { get; init; }
    public required DateTimeOffset LastModified { get; init; }
}
