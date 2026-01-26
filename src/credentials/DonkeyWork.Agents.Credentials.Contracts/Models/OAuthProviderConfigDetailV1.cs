using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// OAuth provider configuration detail.
/// </summary>
public sealed class OAuthProviderConfigDetailV1
{
    /// <summary>
    /// Configuration ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// OAuth provider type.
    /// </summary>
    public required OAuthProvider Provider { get; init; }

    /// <summary>
    /// Client ID (masked).
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Client secret (masked).
    /// </summary>
    public required string ClientSecret { get; init; }

    /// <summary>
    /// Redirect URI.
    /// </summary>
    public required string RedirectUri { get; init; }

    /// <summary>
    /// When the configuration was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
