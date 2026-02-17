using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// The stored context for an OAuth callback, retrieved by looking up the state parameter.
/// </summary>
public sealed class OAuthCallbackState
{
    /// <summary>
    /// The user who initiated the OAuth flow.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// The OAuth provider for this flow.
    /// </summary>
    public required OAuthProvider Provider { get; init; }

    /// <summary>
    /// The PKCE code verifier for this flow.
    /// </summary>
    public required string CodeVerifier { get; init; }
}
