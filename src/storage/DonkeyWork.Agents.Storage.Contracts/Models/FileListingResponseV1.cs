namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class FileListingResponseV1
{
    public required IReadOnlyList<FileItemV1> Files { get; init; }
    public required IReadOnlyList<string> Folders { get; init; }
}
