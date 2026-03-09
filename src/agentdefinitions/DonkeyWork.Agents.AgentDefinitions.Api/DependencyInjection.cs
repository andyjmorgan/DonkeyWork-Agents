using DonkeyWork.Agents.AgentDefinitions.Contracts.Services;
using DonkeyWork.Agents.AgentDefinitions.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.AgentDefinitions.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddAgentDefinitionsApi(this IServiceCollection services)
    {
        services.AddScoped<IAgentDefinitionService, AgentDefinitionService>();
        return services;
    }
}
