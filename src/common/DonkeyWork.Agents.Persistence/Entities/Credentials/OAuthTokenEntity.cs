using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Credentials;

public class OAuthTokenEntity : BaseEntity
{
    public OAuthProvider Provider { get; set; }

    public string ExternalUserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted access token.
    /// </summary>
    public string AccessTokenEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted refresh token.
    /// </summary>
    public string RefreshTokenEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of granted scopes.
    /// </summary>
    public string ScopesJson { get; set; } = "[]";

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? LastRefreshedAt { get; set; }
}
