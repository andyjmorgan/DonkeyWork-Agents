using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using DonkeyWork.Agents.Actors.Core.Providers;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools;

public sealed class AgentToolRegistry
{
    private readonly ILogger<AgentToolRegistry> _logger;
    private readonly FrozenDictionary<string, AgentToolDescriptor> _toolsByName;
    private readonly FrozenDictionary<Type, AgentToolDescriptor[]> _toolsByType;

    public AgentToolRegistry(ILogger<AgentToolRegistry> logger, params Assembly[] assemblies)
    {
        _logger = logger;

        if (assemblies.Length == 0)
        {
            assemblies = [typeof(AgentToolRegistry).Assembly];
        }

        var byName = new Dictionary<string, AgentToolDescriptor>(StringComparer.OrdinalIgnoreCase);
        var byType = new Dictionary<Type, List<AgentToolDescriptor>>();

        foreach (var assembly in assemblies)
        {
            ScanAssembly(assembly, byName, byType);
        }

        _toolsByName = byName.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        _toolsByType = byType.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToArray());

        _logger.LogInformation("AgentToolRegistry initialized with {ToolCount} tools from {TypeCount} types",
            _toolsByName.Count,
            _toolsByType.Count);
    }

    internal IReadOnlyList<InternalToolDefinition> GetToolDefinitions(
        Type[] toolTypes,
        HashSet<Type>? deferredTypes = null,
        HashSet<string>? excludedTools = null)
    {
        var definitions = new List<InternalToolDefinition>();

        foreach (var type in toolTypes)
        {
            if (_toolsByType.TryGetValue(type, out var descriptors))
            {
                var shouldDefer = deferredTypes?.Contains(type) == true;
                foreach (var descriptor in descriptors)
                {
                    if (excludedTools?.Contains(descriptor.Name) == true)
                        continue;

                    var def = descriptor.ToToolDefinition();
                    def.DeferLoading = shouldDefer;
                    definitions.Add(def);
                }
            }
            else
            {
                _logger.LogWarning("No tools found for type {Type}", type.FullName);
            }
        }

        return definitions;
    }

    public async Task<ToolResult> ExecuteAsync(
        string toolName,
        JsonElement input,
        GrainContext context,
        IIdentityContext identityContext,
        IServiceProvider serviceProvider,
        CancellationToken ct,
        Type[]? scopeTypes = null)
    {
        if (!_toolsByName.TryGetValue(toolName, out var descriptor))
        {
            _logger.LogWarning("Tool not found: {ToolName}", toolName);
            return ToolResult.Error($"Tool '{toolName}' not found.");
        }

        if (scopeTypes is not null && scopeTypes.Length > 0)
        {
            var inScope = false;
            foreach (var type in scopeTypes)
            {
                if (type == descriptor.DeclaringType)
                {
                    inScope = true;
                    break;
                }
            }

            if (!inScope)
            {
                _logger.LogWarning("Tool {ToolName} is not in scope for the current execution", toolName);
                return ToolResult.Error($"Tool '{toolName}' is not available in the current context.");
            }
        }

        try
        {
            _logger.LogDebug("Executing tool {ToolName}", toolName);
            var result = await descriptor.ExecuteAsync(input, context, identityContext, serviceProvider, ct);
            _logger.LogDebug("Tool {ToolName} completed, isError={IsError}", toolName, result.IsError);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool {ToolName} threw an exception", toolName);
            return ToolResult.Error($"Tool execution failed: {ex.Message}");
        }
    }

    public bool HasTool(string toolName) =>
        _toolsByName.ContainsKey(toolName);

    public string? GetDisplayName(string toolName) =>
        _toolsByName.TryGetValue(toolName, out var descriptor) ? descriptor.DisplayName : null;

    public IReadOnlyList<string> GetAllToolNames() =>
        _toolsByName.Keys.ToList();

    private void ScanAssembly(
        Assembly assembly,
        Dictionary<string, AgentToolDescriptor> byName,
        Dictionary<Type, List<AgentToolDescriptor>> byType)
    {
        foreach (var type in assembly.GetTypes())
        {
            var descriptors = AgentToolDescriptor.FromType(type);
            if (descriptors.Count == 0)
            {
                continue;
            }

            if (!byType.TryGetValue(type, out var list))
            {
                list = [];
                byType[type] = list;
            }

            foreach (var descriptor in descriptors)
            {
                if (byName.TryGetValue(descriptor.Name, out var existing))
                {
                    _logger.LogWarning(
                        "Duplicate tool name '{ToolName}' in {NewType}, already registered from {ExistingType}. Skipping.",
                        descriptor.Name,
                        type.FullName,
                        existing.DeclaringType.FullName);
                    continue;
                }

                byName[descriptor.Name] = descriptor;
                list.Add(descriptor);
            }
        }
    }
}
