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
    /// Used for browser redirects and token validation.
    /// </summary>
    [Required]
    [Url]
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Internal Keycloak URL for server-to-server calls within the cluster.
    /// If not set, falls back to Authority. Use this to avoid hairpinning issues.
    /// Example: http://keycloak.auth.svc.cluster.local:8080/realms/myrealm
    /// </summary>
    [Url]
    public string? InternalAuthority { get; set; }

    /// <summary>
    /// The expected audience for token validation.
    /// </summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// The OAuth client ID. If not set, defaults to Audience.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The OAuth client secret. Required for confidential clients.
    /// </summary>
    public string? ClientSecret { get; set; }

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
