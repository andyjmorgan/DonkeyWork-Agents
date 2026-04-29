using System.Reflection;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using DonkeyWork.Agents.Mcp.Core;
using DonkeyWork.Agents.Mcp.Core.Middleware;
using DonkeyWork.Agents.Mcp.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore.Authentication;

namespace DonkeyWork.Agents.Mcp.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddMcpApi(this IServiceCollection services, params Assembly[] toolAssemblies)
    {
        services.AddScoped<IMcpServerConfigurationService, McpServerConfigurationService>();
        services.AddScoped<IMcpServerTestService, McpServerTestService>();
        services.AddScoped<IMcpTraceService, McpTraceService>();

        if (toolAssemblies.Length > 0)
        {
            services.AddDynamicMcpServer(toolAssemblies);
        }

        return services;
    }

    public static WebApplication UseMcpApi(this WebApplication app)
    {
        app.UseMiddleware<McpTraceMiddleware>();
        app.MapMcp().RequireAuthorization(new AuthorizeAttribute
        {
            AuthenticationSchemes = McpAuthenticationDefaults.AuthenticationScheme,
        });
        return app;
    }
}
