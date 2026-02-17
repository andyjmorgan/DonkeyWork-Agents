using System.Security.Cryptography;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Credentials.Core.Utilities;

namespace DonkeyWork.Agents.Credentials.Core.Services;

/// <summary>
/// Service for orchestrating OAuth authorization flows.
/// </summary>
public sealed class OAuthFlowService : IOAuthFlowService
{
    private readonly IOAuthProviderConfigService _providerConfigService;
    private readonly IOAuthTokenService _tokenService;
    private readonly IOAuthProviderFactory _providerFactory;

    public OAuthFlowService(
        IOAuthProviderConfigService providerConfigService,
        IOAuthTokenService tokenService,
        IOAuthProviderFactory providerFactory)
    {
        _providerConfigService = providerConfigService;
        _tokenService = tokenService;
        _providerFactory = providerFactory;
    }

    public async Task<(string AuthorizationUrl, string State, string CodeVerifier)> GenerateAuthorizationUrlAsync(
        Guid userId,
        OAuthProvider provider,
        CancellationToken cancellationToken = default)
    {
        // Get provider configuration for the user
        var config = await _providerConfigService.GetByProviderAsync(userId, provider, cancellationToken);
        if (config == null)
        {
            throw new InvalidOperationException(
                $"OAuth provider configuration not found for {provider}. Please configure the provider first.");
        }

        // Generate PKCE parameters
        var codeVerifier = PkceUtility.GenerateCodeVerifier();
        var codeChallenge = PkceUtility.GenerateCodeChallenge(codeVerifier);

        // Generate state for CSRF protection
        var state = GenerateState();

        // Get provider instance (pass config for custom providers)
        var oauthProvider = _providerFactory.GetProvider(provider, config);

        // Build authorization URL
        var authorizationUrl = oauthProvider.BuildAuthorizationUrl(
            config.ClientId,
            config.RedirectUri,
            codeChallenge,
            state);

        return (authorizationUrl, state, codeVerifier);
    }

    public async Task<OAuthToken> HandleCallbackAsync(
        Guid userId,
        OAuthProvider provider,
        string code,
        string state,
        string codeVerifier,
        CancellationToken cancellationToken = default)
    {
        // Get provider configuration
        var config = await _providerConfigService.GetByProviderAsync(userId, provider, cancellationToken);
        if (config == null)
        {
            throw new InvalidOperationException(
                $"OAuth provider configuration not found for {provider}.");
        }

        // Get provider instance (pass config for custom providers)
        var oauthProvider = _providerFactory.GetProvider(provider, config);

        // Exchange code for tokens
        var tokenResponse = await oauthProvider.ExchangeCodeForTokensAsync(
            code,
            codeVerifier,
            config.ClientId,
            config.ClientSecret,
            config.RedirectUri,
            cancellationToken);

        // Get user information
        var userInfo = await oauthProvider.GetUserInfoAsync(
            tokenResponse.AccessToken,
            cancellationToken);

        // Calculate expiration time
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        // Store or update token
        var token = await _tokenService.StoreTokenAsync(
            userId,
            provider,
            userInfo.ExternalUserId,
            userInfo.Email,
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken ?? string.Empty,
            tokenResponse.Scopes ?? oauthProvider.GetDefaultScopes(),
            expiresAt,
            cancellationToken);

        return token;
    }

    private static string GenerateState()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
    }
}
