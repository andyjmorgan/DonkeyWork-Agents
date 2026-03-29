using DonkeyWork.Agents.A2a.Contracts.Services;
using DonkeyWork.Agents.A2a.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.A2a.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddA2aApi(this IServiceCollection services)
    {
        services.AddScoped<IA2aServerConfigurationService, A2aServerConfigurationService>();
        services.AddScoped<IA2aServerTestService, A2aServerTestService>();
        return services;
    }
}
