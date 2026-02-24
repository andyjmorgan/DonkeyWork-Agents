using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// OAuth provider configuration list item.
/// </summary>
public sealed class OAuthProviderConfigItemV1
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
    /// Redirect URI.
    /// </summary>
    public required string RedirectUri { get; init; }

    /// <summary>
    /// When the configuration was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Whether a token exists for this provider.
    /// </summary>
    public required bool HasToken { get; init; }

    /// <summary>
    /// Display name for custom providers.
    /// </summary>
    public string? CustomProviderName { get; init; }

    /// <summary>
    /// Configured scopes for this provider.
    /// </summary>
    public IReadOnlyList<string>? Scopes { get; init; }
}
