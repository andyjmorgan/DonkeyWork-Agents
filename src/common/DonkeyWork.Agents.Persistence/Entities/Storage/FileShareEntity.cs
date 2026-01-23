using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Storage;

public class FileShareEntity : BaseEntity
{
    public Guid FileId { get; set; }

    public string ShareToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public ShareStatus Status { get; set; } = ShareStatus.Active;

    public int? MaxDownloads { get; set; }

    public int DownloadCount { get; set; }

    public string? PasswordHash { get; set; }

    public StoredFileEntity File { get; set; } = null!;
}
