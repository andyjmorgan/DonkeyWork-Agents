using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Actors.Core.Middleware;
using DonkeyWork.Agents.Actors.Core.Providers;
using DonkeyWork.Agents.Actors.Core.Services;
using DonkeyWork.Agents.Actors.Core.Tools;
using DonkeyWork.Agents.Actors.Core.Tools.Swarm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddActorsCore(this IServiceCollection services)
    {
        services.AddSingleton<IAiProviderFactory, AiProviderFactory>();
        services.AddSingleton(sp => new AgentToolRegistry(sp.GetRequiredService<ILogger<AgentToolRegistry>>()));
        services.AddSingleton(sp => new AgentContractRegistry(sp.GetRequiredService<ILogger<AgentContractRegistry>>()));
        services.AddSingleton<IToolGroupService, ToolGroupService>();
        services.AddTransient<ModelPipeline>();
        services.AddScoped<GrainContext>();
        services.AddSingleton<SwarmAgentSpawner>();

        return services;
    }
}
