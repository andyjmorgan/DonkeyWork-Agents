using System.Reflection;
using DonkeyWork.Agents.Actions.Contracts.Attributes;
using DonkeyWork.Agents.Actions.Contracts.Services;
using DonkeyWork.Agents.Actions.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Actions.Api;

/// <summary>
/// Extension methods for registering Actions API services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add Actions API services and controllers
    /// </summary>
    public static IServiceCollection AddActionsApi(this IServiceCollection services)
    {
        // Register core services
        services.AddScoped<IActionSchemaService, ActionSchemaService>();
        services.AddScoped<IExpressionEngine, ScribanExpressionEngine>();
        services.AddScoped<IParameterResolver, ParameterResolverService>();

        // Registry is singleton (discovery happens once at startup)
        // Executor is scoped (can resolve scoped action providers)
        services.AddSingleton<IActionRegistry, ActionRegistry>();
        services.AddScoped<IActionExecutor, ActionExecutorService>();

        // Register HTTP client for HTTP action provider
        services.AddHttpClient();

        // Discover and register action providers
        RegisterActionProviders(services);

        // Controllers are registered automatically by AddControllers() in Program.cs

        return services;
    }

    /// <summary>
    /// Scan the Actions.Core assembly and register all action providers.
    /// </summary>
    private static void RegisterActionProviders(IServiceCollection services)
    {
        var assembly = typeof(ActionSchemaService).Assembly; // Actions.Core assembly

        var providerTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ActionProviderAttribute>() != null && !t.IsAbstract)
            .ToList();

        foreach (var providerType in providerTypes)
        {
            services.AddScoped(providerType);
        }
    }
}
