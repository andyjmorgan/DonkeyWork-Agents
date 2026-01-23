namespace DonkeyWork.Agents.Persistence.Entities.Credentials;

public class ExternalApiKeyEntity : BaseEntity
{
    /// <summary>
    /// Provider name stored as string. Maps to ExternalApiKeyProvider enum in Contracts.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized dictionary of credential fields.
    /// Encrypted at column level in database.
    /// </summary>
    public string FieldsEncrypted { get; set; } = string.Empty;

    public DateTimeOffset? LastUsedAt { get; set; }
}
