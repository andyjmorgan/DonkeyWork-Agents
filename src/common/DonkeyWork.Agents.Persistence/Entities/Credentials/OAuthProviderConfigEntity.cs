using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Credentials;

public class OAuthProviderConfigEntity : BaseEntity
{
    public OAuthProvider Provider { get; set; }

    /// <summary>
    /// Encrypted client ID.
    /// </summary>
    public string ClientIdEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted client secret.
    /// </summary>
    public string ClientSecretEncrypted { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;
}
