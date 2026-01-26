using System.Text.Json;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;

namespace DonkeyWork.Agents.Credentials.Core.Providers;

/// <summary>
/// OAuth provider implementation for Google APIs.
/// </summary>
public sealed class GoogleOAuthProvider : OAuthProviderBase
{
    public GoogleOAuthProvider(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory)
    {
    }

    public override OAuthProvider Provider => OAuthProvider.Google;

    protected override string AuthorizationEndpoint => "https://accounts.google.com/o/oauth2/v2/auth";
    protected override string TokenEndpoint => "https://oauth2.googleapis.com/token";
    protected override string UserInfoEndpoint => "https://www.googleapis.com/oauth2/v2/userinfo";

    public override IEnumerable<string> GetDefaultScopes()
    {
        return new[]
        {
            "openid",
            "profile",
            "email",
            "https://www.googleapis.com/auth/drive.file"
        };
    }

    protected override void AddCustomAuthorizationParameters(Dictionary<string, string> parameters)
    {
        // Required to get refresh token from Google
        parameters["access_type"] = "offline";
        parameters["prompt"] = "consent";
    }

    public override async Task<OAuthUserInfo> GetUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        var json = await GetAuthenticatedAsync(UserInfoEndpoint, accessToken, cancellationToken);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var id = root.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("User ID not found in response");

        var email = root.GetProperty("email").GetString()
            ?? throw new InvalidOperationException("Email not found in Google user info");

        var name = root.TryGetProperty("name", out var nameProp)
            ? nameProp.GetString()
            : null;

        return new OAuthUserInfo(id, email, name);
    }
}
