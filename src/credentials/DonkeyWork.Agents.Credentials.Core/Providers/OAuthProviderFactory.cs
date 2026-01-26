using DonkeyWork.Agents.Common.Contracts.Enums;
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

    public IOAuthProvider GetProvider(OAuthProvider provider)
    {
        return provider switch
        {
            OAuthProvider.Microsoft => _serviceProvider.GetRequiredService<MicrosoftGraphOAuthProvider>(),
            OAuthProvider.Google => _serviceProvider.GetRequiredService<GoogleOAuthProvider>(),
            OAuthProvider.GitHub => _serviceProvider.GetRequiredService<GitHubOAuthProvider>(),
            _ => throw new ArgumentException($"Unsupported OAuth provider: {provider}", nameof(provider))
        };
    }
}
