namespace DonkeyWork.Agents.Identity.Contracts.Services;

/// <summary>
/// Scoped service providing access to the current authenticated user's identity.
/// Populated from either Keycloak JWT or API key authentication.
/// </summary>
public interface IIdentityContext
{
    /// <summary>
    /// The authenticated user's unique identifier.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// The user's display name.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// The user's username (preferred_username from Keycloak).
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
