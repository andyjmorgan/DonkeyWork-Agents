using System.Text.Json;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Services;

namespace DonkeyWork.Agents.Credentials.Core.Providers;

/// <summary>
/// OAuth provider implementation for Microsoft Graph API.
/// </summary>
public sealed class MicrosoftGraphOAuthProvider : OAuthProviderBase
{
    public MicrosoftGraphOAuthProvider(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory)
    {
    }

    public override OAuthProvider Provider => OAuthProvider.Microsoft;

    protected override string AuthorizationEndpoint => "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
    protected override string TokenEndpoint => "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    protected override string UserInfoEndpoint => "https://graph.microsoft.com/v1.0/me";

    public override IEnumerable<string> GetDefaultScopes()
    {
        return new[]
        {
            "openid",
            "offline_access",
            "profile",
            "email",
            "User.Read",
            "Files.ReadWrite.All"
        };
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

        var email = root.TryGetProperty("mail", out var mailProp)
            ? mailProp.GetString()
            : root.TryGetProperty("userPrincipalName", out var upnProp)
                ? upnProp.GetString()
                : null;

        if (string.IsNullOrEmpty(email))
        {
            throw new InvalidOperationException("Email not found in Microsoft Graph user info");
        }

        var name = root.TryGetProperty("displayName", out var nameProp)
            ? nameProp.GetString()
            : null;

        return new OAuthUserInfo(id, email, name);
    }
}
