using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Storage.Contracts.Models;

public sealed class CreateShareRequestV1
{
    [Required]
    public Guid FileId { get; init; }

    /// <summary>
    /// Expiration time in ISO 8601 duration format (e.g., "P1D" for 1 day, "PT1H" for 1 hour).
    /// Defaults to 1 day if not specified.
    /// </summary>
    public string? ExpiresIn { get; init; }

    /// <summary>
    /// Maximum number of downloads allowed. Null means unlimited.
    /// </summary>
    public int? MaxDownloads { get; init; }

    /// <summary>
    /// Optional password to protect the share.
    /// </summary>
    public string? Password { get; init; }
}
