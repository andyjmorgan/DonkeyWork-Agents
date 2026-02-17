using System.Net.Http.Headers;
using System.Text.Json;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;

namespace DonkeyWork.Agents.Credentials.Core.Providers;

/// <summary>
/// Abstract base class for OAuth provider implementations.
/// </summary>
public abstract class OAuthProviderBase : IOAuthProvider
{
    protected readonly IHttpClientFactory HttpClientFactory;

    protected OAuthProviderBase(IHttpClientFactory httpClientFactory)
    {
        HttpClientFactory = httpClientFactory;
    }

    public abstract OAuthProvider Provider { get; }

    protected abstract string AuthorizationEndpoint { get; }
    protected abstract string TokenEndpoint { get; }
    protected abstract string UserInfoEndpoint { get; }

    public virtual string BuildAuthorizationUrl(
        string clientId,
        string redirectUri,
        string codeChallenge,
        string state,
        IEnumerable<string>? scopes = null)
    {
        var scopeList = scopes?.ToList() ?? GetDefaultScopes().ToList();
        var scopeString = string.Join(" ", scopeList);

        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["scope"] = scopeString
        };

        AddCustomAuthorizationParameters(parameters);

        var queryString = string.Join("&", parameters.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{AuthorizationEndpoint}?{queryString}";
    }

    public virtual async Task<OAuthTokenResponse> ExchangeCodeForTokensAsync(
        string code,
        string codeVerifier,
        string clientId,
        string clientSecret,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier
        };

        AddCustomTokenRequestParameters(tokenRequest);

        return await RequestTokenAsync(tokenRequest, cancellationToken);
    }

    public virtual async Task<OAuthTokenResponse> RefreshTokenAsync(
        string refreshToken,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["refresh_token"] = refreshToken
        };

        AddCustomRefreshRequestParameters(tokenRequest);

        return await RequestTokenAsync(tokenRequest, cancellationToken);
    }

    public abstract Task<OAuthUserInfo> GetUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken);

    public abstract IEnumerable<string> GetDefaultScopes();

    /// <summary>
    /// Allows derived classes to add custom authorization parameters.
    /// </summary>
    protected virtual void AddCustomAuthorizationParameters(Dictionary<string, string> parameters)
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Allows derived classes to add custom token request parameters.
    /// </summary>
    protected virtual void AddCustomTokenRequestParameters(Dictionary<string, string> parameters)
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Allows derived classes to add custom refresh request parameters.
    /// </summary>
    protected virtual void AddCustomRefreshRequestParameters(Dictionary<string, string> parameters)
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Makes a token request to the provider's token endpoint.
    /// </summary>
    protected virtual async Task<OAuthTokenResponse> RequestTokenAsync(
        Dictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        var httpClient = HttpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(parameters)
        };

        // Some providers require Accept header
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Token request failed with status {response.StatusCode}: {errorContent}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseTokenResponse(json);
    }

    /// <summary>
    /// Parses the token response JSON. Can be overridden for provider-specific parsing.
    /// </summary>
    protected virtual OAuthTokenResponse ParseTokenResponse(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Some providers (e.g. GitHub) return errors with HTTP 200
        if (root.TryGetProperty("error", out var errorProp))
        {
            var error = errorProp.GetString();
            var description = root.TryGetProperty("error_description", out var descProp)
                ? descProp.GetString()
                : null;
            throw new HttpRequestException(
                $"Token endpoint returned error: {error}" +
                (description != null ? $" - {description}" : string.Empty));
        }

        if (!root.TryGetProperty("access_token", out var atProp) || string.IsNullOrEmpty(atProp.GetString()))
        {
            throw new InvalidOperationException("Token response does not contain access_token");
        }

        var accessToken = atProp.GetString()!;

        var refreshToken = root.TryGetProperty("refresh_token", out var rtProp)
            ? rtProp.GetString()
            : null;

        int? expiresIn = root.TryGetProperty("expires_in", out var exProp)
            ? exProp.GetInt32()
            : null;

        var tokenType = root.TryGetProperty("token_type", out var ttProp)
            ? ttProp.GetString()
            : "Bearer";

        IEnumerable<string>? scopes = null;
        if (root.TryGetProperty("scope", out var scopeProp))
        {
            var scopeString = scopeProp.GetString();
            if (!string.IsNullOrEmpty(scopeString))
            {
                scopes = scopeString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            }
        }

        return new OAuthTokenResponse(accessToken, refreshToken, expiresIn, tokenType, scopes);
    }

    /// <summary>
    /// Makes an authenticated HTTP GET request to an API endpoint.
    /// </summary>
    protected async Task<string> GetAuthenticatedAsync(
        string url,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var httpClient = HttpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("DonkeyWork-Agents", "1.0"));

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"API request to {url} failed with status {response.StatusCode}: {errorContent}");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
