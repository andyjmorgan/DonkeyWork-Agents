using DonkeyWork.Agents.Providers.Core.Middleware;
using DonkeyWork.Agents.Providers.Core.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Providers.Core;

/// <summary>
/// Dependency injection extensions for the Providers Core library.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the internal provider services to the service collection.
    /// </summary>
    public static IServiceCollection AddProvidersCore(this IServiceCollection services)
    {
        // AI Client Factory (internal, placeholder for now)
        services.AddSingleton<IAiClientFactory, PlaceholderAiClientFactory>();

        // Internal middleware (transient - resolved by pipeline)
        services.AddTransient<BaseExceptionMiddleware>();
        services.AddTransient<ToolMiddleware>();
        services.AddTransient<GuardrailsMiddleware>();
        services.AddTransient<AccumulatorMiddleware>();
        services.AddTransient<ProviderMiddleware>();

        return services;
    }
}
