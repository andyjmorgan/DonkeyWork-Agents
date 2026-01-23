using DonkeyWork.Agents.Providers.Contracts.Services;
using DonkeyWork.Agents.Providers.Core;
using DonkeyWork.Agents.Providers.Core.Middleware;
using DonkeyWork.Agents.Providers.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Providers.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddProvidersApi(this IServiceCollection services)
    {
        // Existing services
        services.AddSingleton<IModelCatalogService, ModelCatalogService>();

        // Model Pipeline (public interface, transient for fresh instances per request)
        services.AddTransient<IModelPipeline, ModelPipeline>();

        // Register internal Core services
        services.AddProvidersCore();

        return services;
    }
}
