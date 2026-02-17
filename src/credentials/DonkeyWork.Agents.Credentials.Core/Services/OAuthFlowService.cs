using System.Security.Cryptography;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Credentials.Core.Utilities;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Credentials.Core.Services;

/// <summary>
/// Service for orchestrating OAuth authorization flows.
/// </summary>
public sealed class OAuthFlowService : IOAuthFlowService
{
    private readonly IOAuthProviderConfigService _providerConfigService;
    private readonly IOAuthTokenService _tokenService;
    private readonly IOAuthProviderFactory _providerFactory;
    private readonly AgentsDbContext _dbContext;

    /// <summary>
    /// How long a state parameter is valid for.
    /// </summary>
    private static readonly TimeSpan StateExpiration = TimeSpan.FromMinutes(10);

    public OAuthFlowService(
        IOAuthProviderConfigService providerConfigService,
        IOAuthTokenService tokenService,
        IOAuthProviderFactory providerFactory,
        AgentsDbContext dbContext)
    {
        _providerConfigService = providerConfigService;
        _tokenService = tokenService;
        _providerFactory = providerFactory;
        _dbContext = dbContext;
    }

    public async Task<(string AuthorizationUrl, string State)> GenerateAuthorizationUrlAsync(
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

        // Store state in database
        var stateEntity = new OAuthStateEntity
        {
            UserId = userId,
            State = state,
            Provider = provider,
            CodeVerifier = codeVerifier,
            ExpiresAt = DateTimeOffset.UtcNow.Add(StateExpiration)
        };

        _dbContext.OAuthStates.Add(stateEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Get provider instance (pass config for custom providers)
        var oauthProvider = _providerFactory.GetProvider(provider, config);

        // Build authorization URL
        var authorizationUrl = oauthProvider.BuildAuthorizationUrl(
            config.ClientId,
            config.RedirectUri,
            codeChallenge,
            state);

        return (authorizationUrl, state);
    }

    public async Task<OAuthCallbackState?> ValidateAndConsumeStateAsync(
        string state,
        CancellationToken cancellationToken = default)
    {
        // Look up state - must bypass query filter since callback is anonymous
        var entity = await _dbContext.OAuthStates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.State == state, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        // Check expiration
        if (entity.ExpiresAt < DateTimeOffset.UtcNow)
        {
            // Expired - clean it up and return null
            _dbContext.OAuthStates.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        // Consume the state (one-time use)
        _dbContext.OAuthStates.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new OAuthCallbackState
        {
            UserId = entity.UserId,
            Provider = entity.Provider,
            CodeVerifier = entity.CodeVerifier
        };
    }

    public async Task<OAuthToken> HandleCallbackAsync(
        Guid userId,
        OAuthProvider provider,
        string code,
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

        // Calculate expiration time (null means the token does not expire)
        DateTimeOffset? expiresAt = tokenResponse.ExpiresIn.HasValue
            ? DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn.Value)
            : null;

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
