namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class UploadFileRequest
{
    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required Stream Content { get; init; }

    public Dictionary<string, string>? Metadata { get; init; }
}
