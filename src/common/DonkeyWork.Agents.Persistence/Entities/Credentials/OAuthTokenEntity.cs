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
    public byte[] AccessTokenEncrypted { get; set; } = [];

    /// <summary>
    /// Encrypted refresh token.
    /// </summary>
    public byte[] RefreshTokenEncrypted { get; set; } = [];

    /// <summary>
    /// JSON array of granted scopes.
    /// </summary>
    public string ScopesJson { get; set; } = "[]";

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? LastRefreshedAt { get; set; }
}
