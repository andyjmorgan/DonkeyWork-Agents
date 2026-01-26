namespace DonkeyWork.Agents.Credentials.Contracts.Models;

/// <summary>
/// Response containing the OAuth authorization URL.
/// </summary>
public sealed class GetAuthorizationUrlResponseV1
{
    /// <summary>
    /// The authorization URL to redirect the user to.
    /// </summary>
    public required string AuthorizationUrl { get; init; }

    /// <summary>
    /// The state parameter for CSRF protection.
    /// </summary>
    public required string State { get; init; }
}
