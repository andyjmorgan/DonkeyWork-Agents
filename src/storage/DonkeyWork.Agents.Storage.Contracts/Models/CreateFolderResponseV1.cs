namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class CreateFolderResponseV1
{
    public required string Path { get; init; }
    public required string Name { get; init; }
}
