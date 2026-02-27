using System.Reflection;
using DonkeyWork.Agents.Mcp.Contracts.Handlers;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using DonkeyWork.Agents.Mcp.Core.Handlers;
using DonkeyWork.Agents.Mcp.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Core;

using Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for MCP server configuration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Maps the dynamic MCP server to the web application.
    /// </summary>
    /// <param name="app">The app</param>
    /// <returns></returns>
    public static void MapDynamicMcpServer(this WebApplication app)
    {
        app.MapMcp().RequireAuthorization();
    }

    /// <summary>
    /// Adds MCP server with HTTP transport and discovers tools from the specified assemblies.
    /// Uses custom handlers for tool listing and invocation with support for filtering and logging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="toolAssemblies">Assemblies to scan for MCP tools.</param>
    /// <returns>The MCP server builder for further configuration.</returns>
    public static IMcpServerBuilder AddDynamicMcpServer(this IServiceCollection services, params Assembly[] toolAssemblies)
    {
        // Register the singleton tool registry and initialize it
        services.AddSingleton<McpToolRegistry>();
        services.AddSingleton<IMcpToolRegistry>(sp =>
        {
            var registry = sp.GetRequiredService<McpToolRegistry>();
            registry.Initialize(toolAssemblies);
            return registry;
        });

        // Register scoped services
        services.AddScoped<IMcpToolDiscoveryService, McpToolDiscoveryService>();
        services.AddScoped<IMcpToolExecutor, McpToolExecutor>();

        // Register scoped handlers
        services.AddScoped<IListToolsHandler, ListToolsHandler>();
        services.AddScoped<ICallToolHandler, CallToolHandler>();

        var builder = services
            .AddMcpServer()
            .WithHttpTransport(options =>
            {
                // Use stateless mode so that request-scoped services (like IIdentityContext)
                // are resolved from HttpContext.RequestServices instead of applicationServices.
                // This ensures the authenticated user context is available in MCP tool handlers.
                options.Stateless = true;
            });

        // Configure custom handlers for tool operations
        // The handlers delegate to the scoped services resolved from the request context
        builder.WithListToolsHandler(ListToolsHandlerDelegate);
        builder.WithCallToolHandler(CallToolHandlerDelegate);

        return builder;
    }

    private static async ValueTask<ListToolsResult> ListToolsHandlerDelegate(
        RequestContext<ListToolsRequestParams> context,
        CancellationToken cancellationToken)
    {
        var handler = context.Services?.GetRequiredService<IListToolsHandler>()
            ?? throw new InvalidOperationException("IListToolsHandler not registered");

        return await handler.HandleAsync(context, cancellationToken);
    }

    private static async ValueTask<CallToolResult> CallToolHandlerDelegate(
        RequestContext<CallToolRequestParams> context,
        CancellationToken cancellationToken)
    {
        var handler = context.Services?.GetRequiredService<ICallToolHandler>()
            ?? throw new InvalidOperationException("ICallToolHandler not registered");

        return await handler.HandleAsync(context, cancellationToken);
    }
}
