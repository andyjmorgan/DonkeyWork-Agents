using System.ComponentModel.DataAnnotations;
using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Request to create an OAuth provider configuration.
/// </summary>
public sealed class CreateOAuthProviderConfigRequestV1
{
    /// <summary>
    /// OAuth provider type.
    /// </summary>
    [Required]
    public required OAuthProvider Provider { get; init; }

    /// <summary>
    /// OAuth client ID from the provider.
    /// </summary>
    [Required]
    public required string ClientId { get; init; }

    /// <summary>
    /// OAuth client secret from the provider.
    /// </summary>
    [Required]
    public required string ClientSecret { get; init; }

    /// <summary>
    /// Redirect URI configured in the OAuth provider.
    /// </summary>
    [Required]
    public required string RedirectUri { get; init; }
}
