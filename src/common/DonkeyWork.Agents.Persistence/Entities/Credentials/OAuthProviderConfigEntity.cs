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

    /// <summary>
    /// Custom authorization endpoint URL. Only used when Provider is Custom.
    /// </summary>
    public string? AuthorizationUrl { get; set; }

    /// <summary>
    /// Custom token endpoint URL. Only used when Provider is Custom.
    /// </summary>
    public string? TokenUrl { get; set; }

    /// <summary>
    /// Custom user info endpoint URL. Only used when Provider is Custom.
    /// </summary>
    public string? UserInfoUrl { get; set; }

    /// <summary>
    /// JSON array of scopes. Only used when Provider is Custom.
    /// </summary>
    public string? ScopesJson { get; set; }

    /// <summary>
    /// Display name for the custom provider.
    /// </summary>
    public string? CustomProviderName { get; set; }
}
