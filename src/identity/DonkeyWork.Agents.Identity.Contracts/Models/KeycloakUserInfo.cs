using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Identity.Contracts.Models;

/// <summary>
/// User information returned from Keycloak's UserInfo endpoint.
/// </summary>
public sealed class KeycloakUserInfo
{
    /// <summary>
    /// The user's unique identifier (subject claim).
    /// </summary>
    [JsonPropertyName("sub")]
    public string Sub { get; init; } = string.Empty;

    /// <summary>
    /// The user's email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>
    /// Whether the email has been verified.
    /// </summary>
    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; init; }

    /// <summary>
    /// The user's full name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// The user's preferred username.
    /// </summary>
    [JsonPropertyName("preferred_username")]
    public string? PreferredUsername { get; init; }

    /// <summary>
    /// The user's given (first) name.
    /// </summary>
    [JsonPropertyName("given_name")]
    public string? GivenName { get; init; }

    /// <summary>
    /// The user's family (last) name.
    /// </summary>
    [JsonPropertyName("family_name")]
    public string? FamilyName { get; init; }
}
