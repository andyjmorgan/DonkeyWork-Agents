using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Credentials;

/// <summary>
/// Stores OAuth flow state for secure callback validation.
/// Maps the random state parameter to the user, provider, and PKCE code verifier.
/// </summary>
public class OAuthStateEntity : BaseEntity
{
    /// <summary>
    /// The random state string passed through the OAuth redirect.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth provider this flow is for.
    /// </summary>
    public OAuthProvider Provider { get; set; }

    /// <summary>
    /// The PKCE code verifier for this flow.
    /// </summary>
    public string CodeVerifier { get; set; } = string.Empty;

    /// <summary>
    /// When this state expires and can no longer be used.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
