using System.Collections.Concurrent;
using System.Reflection;
using DonkeyWork.Agents.Mcp.Contracts;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Mcp.Core.Services;

/// <summary>
/// Singleton registry that discovers and caches MCP tools from assemblies.
/// </summary>
public class McpToolRegistry : IMcpToolRegistry
{
    private readonly ILogger<McpToolRegistry> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, McpServerTool> _toolsByName;
    private readonly List<McpServerTool> _allTools;
    private readonly object _initLock = new();
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpToolRegistry"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="serviceProvider">The service provider for DI resolution.</param>
    public McpToolRegistry(
        ILogger<McpToolRegistry> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _toolsByName = new ConcurrentDictionary<string, McpServerTool>(StringComparer.Ordinal);
        _allTools = new List<McpServerTool>();
    }

    /// <summary>
    /// Initializes the registry by discovering tools from the specified assemblies.
    /// This method is idempotent and will only perform discovery once.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for tools.</param>
    public void Initialize(params Assembly[] assemblies)
    {
        lock (_initLock)
        {
            if (_initialized)
            {
                _logger.LogDebug("Tool registry already initialized. Skipping re-initialization.");
                return;
            }

            foreach (var assembly in assemblies)
            {
                DiscoverToolsFromAssembly(assembly);
            }

            _initialized = true;
            _logger.LogInformation("Tool registry initialized. Found {ToolCount} tools", _allTools.Count);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<McpServerTool> GetAllTools()
    {
        EnsureInitialized();
        return _allTools.AsReadOnly();
    }

    /// <inheritdoc />
    public McpServerTool? GetTool(string name)
    {
        EnsureInitialized();
        return _toolsByName.TryGetValue(name, out var tool) ? tool : null;
    }

    private void DiscoverToolsFromAssembly(Assembly assembly)
    {
        _logger.LogDebug("Scanning assembly {AssemblyName} for MCP tools", assembly.GetName().Name);

        var toolTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .ToList();

        foreach (var toolType in toolTypes)
        {
            DiscoverToolsFromType(toolType);
        }
    }

    private void DiscoverToolsFromType(Type toolType)
    {
        var providerAttribute = toolType.GetCustomAttribute<McpToolProviderAttribute>();
        var provider = providerAttribute?.Provider ?? McpToolProvider.DonkeyWork;

        _logger.LogDebug(
            "Discovered tool type {TypeName} with provider {Provider}",
            toolType.Name,
            provider);

        var methods = toolType.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        foreach (var method in methods)
        {
            if (method.GetCustomAttribute<McpServerToolAttribute>() is not null)
            {
                CreateAndRegisterTool(toolType, method, provider);
            }
        }
    }

    private void CreateAndRegisterTool(Type toolType, MethodInfo method, McpToolProvider provider)
    {
        try
        {
            McpServerTool tool;

            if (method.IsStatic)
            {
                tool = McpServerTool.Create(
                    method,
                    options: new McpServerToolCreateOptions { Services = _serviceProvider });
            }
            else
            {
                tool = McpServerTool.Create(
                    method,
                    CreateTargetFactory(toolType),
                    options: new McpServerToolCreateOptions { Services = _serviceProvider });
            }

            var toolName = tool.ProtocolTool.Name;

            if (_toolsByName.TryAdd(toolName, tool))
            {
                _allTools.Add(tool);
                _logger.LogDebug(
                    "Registered tool {ToolName} from {TypeName}.{MethodName} (Provider: {Provider})",
                    toolName,
                    toolType.Name,
                    method.Name,
                    provider);
            }
            else
            {
                _logger.LogWarning(
                    "Tool {ToolName} already registered. Skipping duplicate from {TypeName}.{MethodName}",
                    toolName,
                    toolType.Name,
                    method.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create tool from {TypeName}.{MethodName}",
                toolType.Name,
                method.Name);
        }
    }

    private Func<RequestContext<CallToolRequestParams>, object> CreateTargetFactory(Type toolType)
    {
        return ctx =>
        {
            var services = ctx.Services ?? _serviceProvider;
            return ActivatorUtilities.CreateInstance(services, toolType);
        };
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "Tool registry has not been initialized. Call Initialize() first.");
        }
    }
}
