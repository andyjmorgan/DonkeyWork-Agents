using DonkeyWork.Agents.Identity.Contracts.Models;

namespace DonkeyWork.Agents.Identity.Contracts.Services;

/// <summary>
/// Service for backchannel communication with Keycloak.
/// </summary>
public interface IKeycloakService
{
    /// <summary>
    /// Retrieves user information from Keycloak using an access token.
    /// </summary>
    /// <param name="accessToken">The user's access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User information from Keycloak, or null if the request fails.</returns>
    Task<KeycloakUserInfo?> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken = default);
}
