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
}
