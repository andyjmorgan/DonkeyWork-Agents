using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Credentials.Core.BackgroundServices;

/// <summary>
/// Background service that periodically refreshes expiring OAuth tokens.
/// </summary>
public sealed class OAuthTokenRefreshWorker : BackgroundService
{
    private readonly ILogger<OAuthTokenRefreshWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly OAuthOptions _options;

    public OAuthTokenRefreshWorker(
        ILogger<OAuthTokenRefreshWorker> logger,
        IServiceProvider serviceProvider,
        IOptions<OAuthOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OAuth token refresh worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.TokenRefreshCheckInterval, stoppingToken);
                await RefreshExpiringTokensAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OAuth token refresh worker stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OAuth token refresh worker");
            }
        }

        _logger.LogInformation("OAuth token refresh worker stopped");
    }

    private async Task RefreshExpiringTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IOAuthTokenService>();
        var configService = scope.ServiceProvider.GetRequiredService<IOAuthProviderConfigService>();
        var providerFactory = scope.ServiceProvider.GetRequiredService<IOAuthProviderFactory>();

        try
        {
            var expiringTokens = await tokenService.GetExpiringTokensAsync(
                _options.TokenRefreshWindow,
                cancellationToken);

            if (expiringTokens.Count == 0)
            {
                _logger.LogDebug("No tokens require refreshing");
                return;
            }

            _logger.LogInformation("Found {Count} token(s) requiring refresh", expiringTokens.Count);

            foreach (var token in expiringTokens)
            {
                // Skip if recently refreshed to prevent spam
                if (token.LastRefreshedAt.HasValue &&
                    DateTimeOffset.UtcNow - token.LastRefreshedAt.Value < TimeSpan.FromMinutes(5))
                {
                    _logger.LogDebug("Skipping token {TokenId} - recently refreshed", token.Id);
                    continue;
                }

                await RefreshTokenAsync(token.Id, token.UserId, token.Provider, tokenService, configService, providerFactory, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh expiring tokens");
        }
    }

    private async Task RefreshTokenAsync(
        Guid tokenId,
        Guid userId,
        Common.Contracts.Enums.OAuthProvider provider,
        IOAuthTokenService tokenService,
        IOAuthProviderConfigService configService,
        IOAuthProviderFactory providerFactory,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refreshing token {TokenId} for provider {Provider}", tokenId, provider);

            var token = await tokenService.GetByIdAsync(userId, tokenId, cancellationToken);
            if (token == null)
            {
                _logger.LogWarning("Token {TokenId} not found", tokenId);
                return;
            }

            // Skip tokens without a refresh token (provider doesn't support refresh)
            if (string.IsNullOrEmpty(token.RefreshToken))
            {
                _logger.LogDebug("Skipping token {TokenId} - no refresh token available", tokenId);
                return;
            }

            var config = await configService.GetByProviderAsync(userId, provider, cancellationToken);
            if (config == null)
            {
                _logger.LogWarning("Provider configuration not found for {Provider} and user {UserId}", provider, userId);
                return;
            }

            var oauthProvider = providerFactory.GetProvider(provider, config);

            var tokenResponse = await oauthProvider.RefreshTokenAsync(
                token.RefreshToken,
                config.ClientId,
                config.ClientSecret,
                cancellationToken);

            DateTimeOffset? newExpiresAt = tokenResponse.ExpiresIn.HasValue
                ? DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn.Value)
                : null;

            await tokenService.RefreshTokenAsync(
                tokenId,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken ?? token.RefreshToken,
                newExpiresAt,
                cancellationToken);

            _logger.LogInformation("Successfully refreshed token {TokenId}", tokenId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token {TokenId}", tokenId);
            // Continue to next token - don't fail the entire batch
        }
    }
}
