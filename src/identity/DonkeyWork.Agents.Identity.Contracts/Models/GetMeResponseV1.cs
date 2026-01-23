namespace DonkeyWork.Agents.Identity.Contracts.Models;

/// <summary>
/// Response model containing the authenticated user's information.
/// </summary>
public sealed class GetMeResponseV1
{
    /// <summary>
    /// The user's unique identifier from Keycloak.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// The user's display name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// The user's username.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Whether the user is authenticated.
    /// </summary>
    public required bool IsAuthenticated { get; init; }
}
