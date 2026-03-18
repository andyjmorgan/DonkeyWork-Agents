using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using DonkeyWork.Agents.Identity.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Identity.Api.Controllers;

/// <summary>
/// Test authentication endpoints for OAuth2 authorization code flow with PKCE.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly KeycloakOptions _keycloakOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthController> _logger;
    private const string CodeVerifierCookieName = "pkce_code_verifier";

    public AuthController(
        IOptions<KeycloakOptions> keycloakOptions,
        IHttpClientFactory httpClientFactory,
        ILogger<AuthController> logger)
    {
        _keycloakOptions = keycloakOptions.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the effective OAuth client ID (ClientId if set, otherwise Audience).
    /// </summary>
    private string EffectiveClientId => !string.IsNullOrEmpty(_keycloakOptions.ClientId)
        ? _keycloakOptions.ClientId
        : _keycloakOptions.Audience;

    /// <summary>
    /// Initiates the OAuth2 authorization code flow with PKCE.
    /// Redirects the user to Keycloak for authentication.
    /// </summary>
    /// <param name="idpHint">Optional identity provider hint (e.g., "github") to skip Keycloak login screen.</param>
    /// <returns>Redirect to Keycloak authorization endpoint.</returns>
    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login([FromQuery] string? idpHint = null)
    {
        // Generate PKCE code verifier and challenge
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        // Store code verifier in a secure cookie for the callback
        Response.Cookies.Append(CodeVerifierCookieName, codeVerifier, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10)
        });

        // Build the redirect URI for the callback
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/v1/auth/callback";

        // Build the Keycloak authorization URL
        var authorizationUrl = $"{_keycloakOptions.Authority}/protocol/openid-connect/auth?" +
            $"client_id={Uri.EscapeDataString(EffectiveClientId)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString("openid profile email")}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            $"&code_challenge_method=S256";

        // Add identity provider hint if specified (e.g., "github" to skip Keycloak login screen)
        if (!string.IsNullOrEmpty(idpHint))
        {
            authorizationUrl += $"&kc_idp_hint={Uri.EscapeDataString(idpHint)}";
        }

        return Redirect(authorizationUrl);
    }

    /// <summary>
    /// Handles the OAuth2 callback from Keycloak.
    /// Exchanges the authorization code for tokens using PKCE.
    /// Redirects to the frontend with tokens in the URL fragment.
    /// </summary>
    /// <param name="code">The authorization code from Keycloak.</param>
    /// <param name="error">Error code if authorization failed.</param>
    /// <param name="error_description">Error description if authorization failed.</param>
    /// <returns>Redirect to frontend with tokens or error.</returns>
    /// <response code="302">Redirects to frontend with tokens in URL fragment.</response>
    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? error,
        [FromQuery] string? error_description)
    {
        var frontendBaseUrl = !string.IsNullOrEmpty(_keycloakOptions.FrontendUrl)
            ? _keycloakOptions.FrontendUrl.TrimEnd('/')
            : $"{Request.Scheme}://{Request.Host}";
        var frontendCallbackUrl = $"{frontendBaseUrl}/login/callback";

        // Check for errors from Keycloak
        if (!string.IsNullOrEmpty(error))
        {
            return Redirect($"{frontendCallbackUrl}#error={Uri.EscapeDataString(error)}&error_description={Uri.EscapeDataString(error_description ?? "")}");
        }

        if (string.IsNullOrEmpty(code))
        {
            return Redirect($"{frontendCallbackUrl}#error=missing_code&error_description={Uri.EscapeDataString("Authorization code is required.")}");
        }

        // Retrieve the code verifier from the cookie
        if (!Request.Cookies.TryGetValue(CodeVerifierCookieName, out var codeVerifier) || string.IsNullOrEmpty(codeVerifier))
        {
            return Redirect($"{frontendCallbackUrl}#error=missing_verifier&error_description={Uri.EscapeDataString("PKCE code verifier not found. Please start the login flow again.")}");
        }

        // Clear the code verifier cookie
        Response.Cookies.Delete(CodeVerifierCookieName);

        // Build the redirect URI (must match exactly what was sent in /login)
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/v1/auth/callback";

        // Exchange the authorization code for tokens (use internal URL to avoid hairpinning)
        var backchannelAuthority = _keycloakOptions.InternalAuthority ?? _keycloakOptions.Authority;
        var tokenEndpoint = $"{backchannelAuthority}/protocol/openid-connect/token";

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = EffectiveClientId,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier
        };

        // Add client_secret if configured (required for confidential clients)
        if (!string.IsNullOrEmpty(_keycloakOptions.ClientSecret))
        {
            tokenRequest["client_secret"] = _keycloakOptions.ClientSecret;
        }

        var httpClient = _httpClientFactory.CreateClient();
        var tokenResponse = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
            return Redirect($"{frontendCallbackUrl}#error=token_exchange_failed&error_description={Uri.EscapeDataString(errorContent)}");
        }

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokens = JsonSerializer.Deserialize<TokenResponse>(tokenJson);

        if (tokens == null || string.IsNullOrEmpty(tokens.AccessToken))
        {
            return Redirect($"{frontendCallbackUrl}#error=invalid_token_response&error_description={Uri.EscapeDataString("Failed to parse token response.")}");
        }

        // Build the redirect URL with tokens in the fragment (fragment is not sent to server)
        var fragmentParams = new List<string>
        {
            $"access_token={Uri.EscapeDataString(tokens.AccessToken)}",
            $"expires_in={tokens.ExpiresIn}",
            $"token_type={Uri.EscapeDataString(tokens.TokenType ?? "Bearer")}"
        };

        if (!string.IsNullOrEmpty(tokens.RefreshToken))
        {
            fragmentParams.Add($"refresh_token={Uri.EscapeDataString(tokens.RefreshToken)}");
        }

        return Redirect($"{frontendCallbackUrl}#{string.Join("&", fragmentParams)}");
    }

    /// <summary>
    /// Refreshes the access token using a refresh token.
    /// </summary>
    /// <param name="request">The refresh token request.</param>
    /// <returns>New access and refresh tokens.</returns>
    /// <response code="200">Returns new tokens.</response>
    /// <response code="400">If the refresh token is invalid or expired.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        // Log truncated token for debugging (first 20 chars + length)
        var tokenPreview = string.IsNullOrEmpty(request.RefreshToken)
            ? "(empty)"
            : $"{request.RefreshToken[..Math.Min(20, request.RefreshToken.Length)]}... (len={request.RefreshToken.Length})";
        _logger.LogInformation("Token refresh requested. Token preview: {TokenPreview}", tokenPreview);

        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            _logger.LogWarning("Token refresh failed: refresh token is empty or null");
            return BadRequest(new { error = "refresh_token_required", error_description = "Refresh token is required." });
        }

        var backchannelAuthority = _keycloakOptions.InternalAuthority ?? _keycloakOptions.Authority;
        var tokenEndpoint = $"{backchannelAuthority}/protocol/openid-connect/token";

        _logger.LogDebug("Calling Keycloak token endpoint: {TokenEndpoint}, ClientId: {ClientId}",
            tokenEndpoint, EffectiveClientId);

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = EffectiveClientId,
            ["refresh_token"] = request.RefreshToken
        };

        // Add client_secret if configured (required for confidential clients)
        if (!string.IsNullOrEmpty(_keycloakOptions.ClientSecret))
        {
            tokenRequest["client_secret"] = _keycloakOptions.ClientSecret;
            _logger.LogDebug("Client secret is configured and will be included in request");
        }

        var httpClient = _httpClientFactory.CreateClient();
        var tokenResponse = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(tokenRequest));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();

            // Parse Keycloak error response for better logging
            string? keycloakError = null;
            string? keycloakErrorDescription = null;
            try
            {
                var errorJson = JsonSerializer.Deserialize<JsonElement>(errorContent);
                keycloakError = errorJson.TryGetProperty("error", out var e) ? e.GetString() : null;
                keycloakErrorDescription = errorJson.TryGetProperty("error_description", out var ed) ? ed.GetString() : null;
            }
            catch
            {
                // Ignore JSON parse errors
            }

            _logger.LogWarning(
                "Token refresh failed. Status: {StatusCode}, KeycloakError: {KeycloakError}, " +
                "KeycloakErrorDescription: {KeycloakErrorDescription}, TokenEndpoint: {TokenEndpoint}, " +
                "TokenPreview: {TokenPreview}, RawResponse: {RawResponse}",
                (int)tokenResponse.StatusCode,
                keycloakError ?? "(not parsed)",
                keycloakErrorDescription ?? "(not parsed)",
                tokenEndpoint,
                tokenPreview,
                errorContent);

            return BadRequest(new { error = "refresh_failed", error_description = errorContent });
        }

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokens = JsonSerializer.Deserialize<TokenResponse>(tokenJson);

        if (tokens == null || string.IsNullOrEmpty(tokens.AccessToken))
        {
            _logger.LogWarning("Token refresh failed: could not parse token response");
            return BadRequest(new { error = "invalid_response", error_description = "Failed to parse token response." });
        }

        _logger.LogInformation("Token refresh successful. New token expires in {ExpiresIn} seconds", tokens.ExpiresIn);

        return Ok(new RefreshTokenResponse
        {
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresIn = tokens.ExpiresIn,
            TokenType = tokens.TokenType ?? "Bearer"
        });
    }

    /// <summary>
    /// Logs the user out by ending the Keycloak session.
    /// Redirects to the Keycloak end-session endpoint which clears the SSO cookie,
    /// then redirects back to the frontend login page.
    /// </summary>
    /// <returns>Redirect to Keycloak logout endpoint.</returns>
    /// <response code="302">Redirects to Keycloak logout, then back to frontend login.</response>
    [HttpGet("logout")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Logout()
    {
        var frontendBaseUrl = !string.IsNullOrEmpty(_keycloakOptions.FrontendUrl)
            ? _keycloakOptions.FrontendUrl.TrimEnd('/')
            : $"{Request.Scheme}://{Request.Host}";

        var postLogoutRedirectUri = $"{frontendBaseUrl}/login";

        var logoutUrl = $"{_keycloakOptions.Authority}/protocol/openid-connect/logout?" +
            $"client_id={Uri.EscapeDataString(EffectiveClientId)}" +
            $"&post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";

        return Redirect(logoutUrl);
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    private sealed class KeycloakUserInfo
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("preferred_username")]
        public string? PreferredUsername { get; set; }
    }

    /// <summary>
    /// Request to refresh access token.
    /// </summary>
    public sealed class RefreshTokenRequest
    {
        /// <summary>
        /// The refresh token.
        /// </summary>
        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }
    }

    /// <summary>
    /// Response containing new tokens after refresh.
    /// </summary>
    public sealed class RefreshTokenResponse
    {
        /// <summary>
        /// The new access token.
        /// </summary>
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        /// <summary>
        /// The new refresh token (if provided).
        /// </summary>
        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Token expiration time in seconds.
        /// </summary>
        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// The token type (typically "Bearer").
        /// </summary>
        [JsonPropertyName("tokenType")]
        public string? TokenType { get; set; }
    }
}
