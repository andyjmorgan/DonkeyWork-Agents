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

    /// <summary>
    /// Custom authorization endpoint URL. Only set for Custom providers.
    /// </summary>
    public string? AuthorizationUrl { get; init; }

    /// <summary>
    /// Custom token endpoint URL. Only set for Custom providers.
    /// </summary>
    public string? TokenUrl { get; init; }

    /// <summary>
    /// Custom user info endpoint URL. Only set for Custom providers.
    /// </summary>
    public string? UserInfoUrl { get; init; }

    /// <summary>
    /// Custom scopes. Only set for Custom providers.
    /// </summary>
    public IReadOnlyList<string>? Scopes { get; init; }

    /// <summary>
    /// Display name for the custom provider.
    /// </summary>
    public string? CustomProviderName { get; init; }
}
