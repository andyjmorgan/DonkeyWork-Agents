namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Request to update an OAuth provider configuration.
/// </summary>
public sealed class UpdateOAuthProviderConfigRequestV1
{
    /// <summary>
    /// OAuth client ID (optional, only update if provided).
    /// </summary>
    public string? ClientId { get; init; }

    /// <summary>
    /// OAuth client secret (optional, only update if provided).
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// Redirect URI (optional, only update if provided).
    /// </summary>
    public string? RedirectUri { get; init; }

    /// <summary>
    /// Authorization endpoint URL (for Custom providers).
    /// </summary>
    public string? AuthorizationUrl { get; init; }

    /// <summary>
    /// Token endpoint URL (for Custom providers).
    /// </summary>
    public string? TokenUrl { get; init; }

    /// <summary>
    /// User info endpoint URL (for Custom providers).
    /// </summary>
    public string? UserInfoUrl { get; init; }

    /// <summary>
    /// Scopes (for Custom providers).
    /// </summary>
    public List<string>? Scopes { get; init; }

    /// <summary>
    /// Display name for the custom provider.
    /// </summary>
    public string? CustomProviderName { get; init; }
}
