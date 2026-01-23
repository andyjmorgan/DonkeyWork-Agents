using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Identity.Api.Options;

/// <summary>
/// Configuration options for Keycloak authentication.
/// </summary>
public sealed class KeycloakOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Keycloak";

    /// <summary>
    /// The Keycloak authority URL (e.g., https://auth.example.com/realms/myrealm).
    /// </summary>
    [Required]
    [Url]
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// The expected audience for token validation.
    /// </summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Whether to require HTTPS for metadata retrieval. Default is true.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// The frontend URL for OAuth callback redirects (e.g., http://localhost:5174).
    /// If not set, defaults to the request origin.
    /// </summary>
    [Url]
    public string? FrontendUrl { get; set; }
}
