using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Storage;

public class StoredFileEntity : BaseEntity
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string BucketName { get; set; } = string.Empty;

    public string ObjectKey { get; set; } = string.Empty;

    public string? ChecksumSha256 { get; set; }

    public FileStatus Status { get; set; } = FileStatus.Active;

    public DateTimeOffset? MarkedForDeletionAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string? Metadata { get; set; }

    public ICollection<FileShareEntity> Shares { get; set; } = new List<FileShareEntity>();
}
