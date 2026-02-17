using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Represents an external OAuth token credential.
/// </summary>
public sealed class OAuthToken
{
    public required Guid Id { get; init; }

    public required Guid UserId { get; init; }

    public required OAuthProvider Provider { get; init; }

    /// <summary>
    /// The external user ID from the OAuth provider.
    /// </summary>
    public required string ExternalUserId { get; init; }

    public required string Email { get; init; }

    /// <summary>
    /// The access token (encrypted at rest).
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// The refresh token (encrypted at rest).
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Scopes granted by the user.
    /// </summary>
    public required IReadOnlyList<string> Scopes { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? LastRefreshedAt { get; init; }
}
