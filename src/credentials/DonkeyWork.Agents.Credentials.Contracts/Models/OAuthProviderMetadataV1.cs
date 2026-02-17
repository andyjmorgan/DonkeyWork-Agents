using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Metadata about a known OAuth provider, including endpoint URLs,
/// default scopes, and setup instructions.
/// </summary>
public sealed class OAuthProviderMetadataV1
{
    /// <summary>
    /// The provider type.
    /// </summary>
    public required OAuthProvider Provider { get; init; }

    /// <summary>
    /// Display name for the provider.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Authorization endpoint URL.
    /// </summary>
    public required string AuthorizationUrl { get; init; }

    /// <summary>
    /// Token endpoint URL.
    /// </summary>
    public required string TokenUrl { get; init; }

    /// <summary>
    /// User info endpoint URL.
    /// </summary>
    public required string UserInfoUrl { get; init; }

    /// <summary>
    /// Default scopes requested during authorization.
    /// </summary>
    public required IReadOnlyList<string> DefaultScopes { get; init; }

    /// <summary>
    /// URL where users can create an OAuth application.
    /// </summary>
    public required string SetupUrl { get; init; }

    /// <summary>
    /// Brief setup instructions for the provider.
    /// </summary>
    public required string SetupInstructions { get; init; }

    /// <summary>
    /// Whether this is a built-in provider (vs custom).
    /// </summary>
    public required bool IsBuiltIn { get; init; }
}
