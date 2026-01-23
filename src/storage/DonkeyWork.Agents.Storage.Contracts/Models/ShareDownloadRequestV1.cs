namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class ShareDownloadRequestV1
{
    /// <summary>
    /// Password for protected shares.
    /// </summary>
    public string? Password { get; init; }
}
