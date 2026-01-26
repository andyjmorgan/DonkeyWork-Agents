using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Credentials.Contracts.Services;

/// <summary>
/// Interface for OAuth provider implementations.
/// </summary>
public interface IOAuthProvider
{
    /// <summary>
    /// The OAuth provider type.
    /// </summary>
    OAuthProvider Provider { get; }

    /// <summary>
    /// Builds the authorization URL with PKCE challenge.
    /// </summary>
    /// <param name="clientId">OAuth client ID.</param>
    /// <param name="redirectUri">Callback redirect URI.</param>
    /// <param name="codeChallenge">PKCE code challenge.</param>
    /// <param name="state">State parameter for CSRF protection.</param>
    /// <param name="scopes">Optional scopes to request. If null, default scopes are used.</param>
    /// <returns>The authorization URL to redirect the user to.</returns>
    string BuildAuthorizationUrl(
        string clientId,
        string redirectUri,
        string codeChallenge,
        string state,
        IEnumerable<string>? scopes = null);

    /// <summary>
    /// Exchanges authorization code for access and refresh tokens.
    /// </summary>
    /// <param name="code">Authorization code from callback.</param>
    /// <param name="codeVerifier">PKCE code verifier.</param>
    /// <param name="clientId">OAuth client ID.</param>
    /// <param name="clientSecret">OAuth client secret.</param>
    /// <param name="redirectUri">Callback redirect URI (must match authorization request).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>OAuth token response.</returns>
    Task<OAuthTokenResponse> ExchangeCodeForTokensAsync(
        string code,
        string codeVerifier,
        string clientId,
        string clientSecret,
        string redirectUri,
        CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes an expired access token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="clientId">OAuth client ID.</param>
    /// <param name="clientSecret">OAuth client secret.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New OAuth token response.</returns>
    Task<OAuthTokenResponse> RefreshTokenAsync(
        string refreshToken,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets user information from the OAuth provider.
    /// </summary>
    /// <param name="accessToken">Access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User information from the provider.</returns>
    Task<OAuthUserInfo> GetUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the default scopes for this provider.
    /// </summary>
    /// <returns>Default scopes.</returns>
    IEnumerable<string> GetDefaultScopes();
}

/// <summary>
/// Response from OAuth token exchange or refresh.
/// </summary>
/// <param name="AccessToken">Access token.</param>
/// <param name="RefreshToken">Refresh token (may be null).</param>
/// <param name="ExpiresIn">Token expiration in seconds.</param>
/// <param name="TokenType">Token type (usually "Bearer").</param>
/// <param name="Scopes">Granted scopes.</param>
public record OAuthTokenResponse(
    string AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string? TokenType,
    IEnumerable<string>? Scopes);

/// <summary>
/// User information from OAuth provider.
/// </summary>
/// <param name="ExternalUserId">User ID from the provider.</param>
/// <param name="Email">User's email address.</param>
/// <param name="Name">User's display name (may be null).</param>
public record OAuthUserInfo(
    string ExternalUserId,
    string Email,
    string? Name);
