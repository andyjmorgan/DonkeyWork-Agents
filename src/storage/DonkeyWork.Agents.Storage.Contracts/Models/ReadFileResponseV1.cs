namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class ReadFileResponseV1
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public required string Content { get; init; }
    public required string ContentType { get; init; }
    public required long Size { get; init; }
    public required DateTimeOffset LastModified { get; init; }
}
