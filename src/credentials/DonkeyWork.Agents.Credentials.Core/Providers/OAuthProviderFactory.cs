using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Credentials.Core.Providers;

/// <summary>
/// Factory for creating OAuth provider instances.
/// </summary>
public sealed class OAuthProviderFactory : IOAuthProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public OAuthProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IOAuthProvider GetProvider(OAuthProvider provider, OAuthProviderConfig? config = null)
    {
        return provider switch
        {
            OAuthProvider.Microsoft => _serviceProvider.GetRequiredService<MicrosoftGraphOAuthProvider>(),
            OAuthProvider.Google => _serviceProvider.GetRequiredService<GoogleOAuthProvider>(),
            OAuthProvider.GitHub => _serviceProvider.GetRequiredService<GitHubOAuthProvider>(),
            OAuthProvider.Custom => CreateCustomProvider(config),
            _ => throw new ArgumentException($"Unsupported OAuth provider: {provider}", nameof(provider))
        };
    }

    private CustomOAuthProvider CreateCustomProvider(OAuthProviderConfig? config)
    {
        if (config == null)
        {
            throw new InvalidOperationException(
                "Provider configuration with custom URLs is required for Custom OAuth providers.");
        }

        if (string.IsNullOrEmpty(config.AuthorizationUrl) || string.IsNullOrEmpty(config.TokenUrl))
        {
            throw new InvalidOperationException(
                "Custom OAuth provider requires AuthorizationUrl and TokenUrl to be set.");
        }

        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        return new CustomOAuthProvider(
            httpClientFactory,
            config.AuthorizationUrl,
            config.TokenUrl,
            config.UserInfoUrl,
            config.Scopes?.ToList());
    }
}
