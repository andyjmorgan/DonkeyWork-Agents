namespace DonkeyWork.Agents.Identity.Contracts.Models;

/// <summary>
/// Response model containing the tokens from the OAuth callback.
/// </summary>
public sealed class LoginCallbackResponseV1
{
    /// <summary>
    /// The access token for API requests.
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// The refresh token for obtaining new access tokens.
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// The token type (typically "Bearer").
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// The authenticated user's information.
    /// </summary>
    public required GetMeResponseV1 User { get; init; }
}
