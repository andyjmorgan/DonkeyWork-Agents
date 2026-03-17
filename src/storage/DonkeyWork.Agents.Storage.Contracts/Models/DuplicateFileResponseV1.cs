namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class DuplicateFileResponseV1
{
    public required string Path { get; init; }
    public required string Name { get; init; }
}
