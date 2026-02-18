using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// OAuth token list item.
/// </summary>
public sealed class OAuthTokenItemV1
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
    /// External user ID from the provider.
    /// </summary>
    public required string ExternalUserId { get; init; }

    /// <summary>
    /// Token status.
    /// </summary>
    public required OAuthTokenStatus Status { get; init; }

    /// <summary>
    /// When the token expires. Null if the token does not expire.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// When the token was last refreshed.
    /// </summary>
    public DateTimeOffset? LastRefreshedAt { get; init; }

    /// <summary>
    /// When the token was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Scopes granted by the provider.
    /// </summary>
    public required IReadOnlyList<string> Scopes { get; init; }
}
