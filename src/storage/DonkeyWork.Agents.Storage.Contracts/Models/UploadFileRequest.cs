namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class UploadFileRequest
{
    public required string FileName { get; init; }

    public required string ContentType { get; init; }

    public required Stream Content { get; init; }

    /// <summary>
    /// Optional key prefix within the user's namespace (e.g., "conversations/{convId}").
    /// </summary>
    public string? KeyPrefix { get; init; }
}
