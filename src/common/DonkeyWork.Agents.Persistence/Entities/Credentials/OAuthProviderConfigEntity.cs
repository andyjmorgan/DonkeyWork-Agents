using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Credentials;

public class OAuthProviderConfigEntity : BaseEntity
{
    public OAuthProvider Provider { get; set; }

    /// <summary>
    /// Encrypted client ID.
    /// </summary>
    public byte[] ClientIdEncrypted { get; set; } = [];

    /// <summary>
    /// Encrypted client secret.
    /// </summary>
    public byte[] ClientSecretEncrypted { get; set; } = [];

    public string RedirectUri { get; set; } = string.Empty;
}
