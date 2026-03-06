using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using DonkeyWork.Agents.Credentials.Core.BackgroundServices;
using DonkeyWork.Agents.Credentials.Core.Providers;
using DonkeyWork.Agents.Credentials.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Credentials.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddCredentialsApi(this IServiceCollection services)
    {
        // API Key services
        services.AddScoped<IUserApiKeyService, UserApiKeyService>();
        services.AddScoped<IExternalApiKeyService, ExternalApiKeyService>();

        // OAuth options
        services.AddOptions<OAuthOptions>()
            .BindConfiguration(OAuthOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // OAuth services
        services.AddScoped<IOAuthProviderConfigService, OAuthProviderConfigService>();
        services.AddScoped<IOAuthTokenService, OAuthTokenService>();
        services.AddScoped<IOAuthFlowService, OAuthFlowService>();
        services.AddScoped<IOAuthProviderFactory, OAuthProviderFactory>();

        // OAuth providers
        services.AddScoped<MicrosoftGraphOAuthProvider>();
        services.AddScoped<GoogleOAuthProvider>();
        services.AddScoped<GitHubOAuthProvider>();

        // Sandbox credential mappings
        services.AddScoped<ISandboxCredentialMappingService, SandboxCredentialMappingService>();

        // Background services
        services.AddHostedService<OAuthTokenRefreshWorker>();

        return services;
    }
}
