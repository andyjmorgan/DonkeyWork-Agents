using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Response containing the unmasked OAuth access token.
/// </summary>
public sealed class GetOAuthAccessTokenResponseV1
{
    /// <summary>
    /// Token ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// OAuth provider type.
    /// </summary>
    public required OAuthProvider Provider { get; init; }

    /// <summary>
    /// Email address from the provider.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// The unmasked access token.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Token status.
    /// </summary>
    public required OAuthTokenStatus Status { get; init; }

    /// <summary>
    /// When the token expires. Null if the token does not expire.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}
