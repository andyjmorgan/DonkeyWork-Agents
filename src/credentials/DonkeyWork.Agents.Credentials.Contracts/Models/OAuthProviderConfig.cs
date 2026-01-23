using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Represents OAuth provider configuration for a user.
/// Allows users to bring their own OAuth app credentials.
/// </summary>
public sealed class OAuthProviderConfig
{
    public required Guid Id { get; init; }

    public required Guid UserId { get; init; }

    public required OAuthProvider Provider { get; init; }

    /// <summary>
    /// OAuth client ID (encrypted at rest).
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// OAuth client secret (encrypted at rest).
    /// </summary>
    public required string ClientSecret { get; init; }

    public required string RedirectUri { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
