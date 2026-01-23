using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asp.Versioning;
using DonkeyWork.Agents.Identity.Api.Options;
using DonkeyWork.Agents.Identity.Contracts.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    private const string CodeVerifierCookieName = "pkce_code_verifier";

    public AuthController(
        IOptions<KeycloakOptions> keycloakOptions,
        IHttpClientFactory httpClientFactory)
    {
        _keycloakOptions = keycloakOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initiates the OAuth2 authorization code flow with PKCE.
    /// Redirects the user to Keycloak for authentication.
    /// </summary>
    /// <returns>Redirect to Keycloak authorization endpoint.</returns>
    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Login()
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
            $"client_id={Uri.EscapeDataString(_keycloakOptions.Audience)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString("openid profile email")}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            $"&code_challenge_method=S256";

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

        // Exchange the authorization code for tokens
        var tokenEndpoint = $"{_keycloakOptions.Authority}/protocol/openid-connect/token";

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _keycloakOptions.Audience,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier
        };

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
}
