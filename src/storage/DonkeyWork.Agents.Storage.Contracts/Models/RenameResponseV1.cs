namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class RenameResponseV1
{
    public required string OldPath { get; init; }
    public required string NewPath { get; init; }
    public required string NewName { get; init; }
}
