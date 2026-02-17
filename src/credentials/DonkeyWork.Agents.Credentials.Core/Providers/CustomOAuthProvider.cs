using System.Text.Json;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;

namespace DonkeyWork.Agents.Credentials.Core.Providers;

/// <summary>
/// OAuth provider implementation for custom/user-defined OAuth endpoints.
/// URLs and scopes are provided dynamically rather than hardcoded.
/// </summary>
public sealed class CustomOAuthProvider : OAuthProviderBase
{
    private readonly string _authorizationEndpoint;
    private readonly string _tokenEndpoint;
    private readonly string _userInfoEndpoint;
    private readonly IReadOnlyList<string> _defaultScopes;

    public CustomOAuthProvider(
        IHttpClientFactory httpClientFactory,
        string authorizationEndpoint,
        string tokenEndpoint,
        string? userInfoEndpoint,
        IReadOnlyList<string>? scopes)
        : base(httpClientFactory)
    {
        _authorizationEndpoint = authorizationEndpoint;
        _tokenEndpoint = tokenEndpoint;
        _userInfoEndpoint = userInfoEndpoint ?? string.Empty;
        _defaultScopes = scopes ?? [];
    }

    public override OAuthProvider Provider => OAuthProvider.Custom;

    protected override string AuthorizationEndpoint => _authorizationEndpoint;
    protected override string TokenEndpoint => _tokenEndpoint;
    protected override string UserInfoEndpoint => _userInfoEndpoint;

    public override IEnumerable<string> GetDefaultScopes()
    {
        return _defaultScopes;
    }

    public override async Task<OAuthUserInfo> GetUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_userInfoEndpoint))
        {
            // If no user info endpoint is configured, return a placeholder
            return new OAuthUserInfo("unknown", "unknown@custom", null);
        }

        var json = await GetAuthenticatedAsync(UserInfoEndpoint, accessToken, cancellationToken);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Try common user info field names across different providers
        var id = TryGetStringProperty(root, "id", "sub", "user_id") ?? "unknown";
        var email = TryGetStringProperty(root, "email", "mail", "userPrincipalName") ?? "unknown@custom";
        var name = TryGetStringProperty(root, "name", "displayName", "display_name", "login");

        return new OAuthUserInfo(id, email, name);
    }

    private static string? TryGetStringProperty(JsonElement root, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var value = prop.GetString();
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
        }

        return null;
    }
}
