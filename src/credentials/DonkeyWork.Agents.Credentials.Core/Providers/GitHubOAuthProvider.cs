using System.Text.Json;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;

namespace DonkeyWork.Agents.Credentials.Core.Providers;

/// <summary>
/// OAuth provider implementation for GitHub API.
/// </summary>
public sealed class GitHubOAuthProvider : OAuthProviderBase
{
    public GitHubOAuthProvider(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory)
    {
    }

    public override OAuthProvider Provider => OAuthProvider.GitHub;

    protected override string AuthorizationEndpoint => "https://github.com/login/oauth/authorize";
    protected override string TokenEndpoint => "https://github.com/login/oauth/access_token";
    protected override string UserInfoEndpoint => "https://api.github.com/user";

    public override IEnumerable<string> GetDefaultScopes()
    {
        return new[]
        {
            "user:email",
            "repo"
        };
    }

    public override async Task<OAuthUserInfo> GetUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        var json = await GetAuthenticatedAsync(UserInfoEndpoint, accessToken, cancellationToken);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var id = root.GetProperty("id").GetInt64().ToString();

        // GitHub may not return email in the user endpoint if it's private
        var email = root.TryGetProperty("email", out var emailProp) && !emailProp.ValueEquals("null")
            ? emailProp.GetString()
            : null;

        // If email is not in the main user response, fetch from emails endpoint
        if (string.IsNullOrEmpty(email))
        {
            email = await GetPrimaryEmailAsync(accessToken, cancellationToken);
        }

        if (string.IsNullOrEmpty(email))
        {
            throw new InvalidOperationException("Email not found in GitHub user info");
        }

        var name = root.TryGetProperty("name", out var nameProp)
            ? nameProp.GetString()
            : root.TryGetProperty("login", out var loginProp)
                ? loginProp.GetString()
                : null;

        return new OAuthUserInfo(id, email, name);
    }

    private async Task<string?> GetPrimaryEmailAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            var json = await GetAuthenticatedAsync("https://api.github.com/user/emails", accessToken, cancellationToken);
            var doc = JsonDocument.Parse(json);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("primary", out var primaryProp) &&
                    primaryProp.GetBoolean() &&
                    element.TryGetProperty("verified", out var verifiedProp) &&
                    verifiedProp.GetBoolean() &&
                    element.TryGetProperty("email", out var emailProp))
                {
                    return emailProp.GetString();
                }
            }

            // If no primary email found, return first verified email
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("verified", out var verifiedProp) &&
                    verifiedProp.GetBoolean() &&
                    element.TryGetProperty("email", out var emailProp))
                {
                    return emailProp.GetString();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
