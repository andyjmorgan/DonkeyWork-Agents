using DonkeyWork.Agents.Mcp.Contracts.Services;
using DonkeyWork.Agents.Mcp.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Mcp.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddMcpApi(this IServiceCollection services)
    {
        services.AddScoped<IMcpServerConfigurationService, McpServerConfigurationService>();
        services.AddScoped<IMcpServerTestService, McpServerTestService>();
        services.AddScoped<IMcpTraceService, McpTraceService>();
        return services;
    }
}
