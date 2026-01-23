namespace DonkeyWork.Agents.Persistence;

public class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Encryption key for pgcrypto column-level encryption.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;
}
