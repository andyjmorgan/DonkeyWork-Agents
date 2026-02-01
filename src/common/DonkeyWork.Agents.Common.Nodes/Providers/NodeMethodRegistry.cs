using System.Reflection;
using DonkeyWork.Agents.Common.Nodes.Enums;

namespace DonkeyWork.Agents.Common.Nodes.Providers;

/// <summary>
/// Registry for discovering and accessing node execution methods from provider classes.
/// </summary>
public sealed class NodeMethodRegistry
{
    private readonly Dictionary<NodeType, NodeMethodInfo> _methods = new();

    /// <summary>
    /// Gets all registered node methods.
    /// </summary>
    public IReadOnlyDictionary<NodeType, NodeMethodInfo> Methods => _methods;

    /// <summary>
    /// Discovers node providers and their methods from the specified assembly.
    /// </summary>
    public void DiscoverProviders(Assembly assembly)
    {
        var providerTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<NodeProviderAttribute>() != null && !t.IsAbstract);

        foreach (var providerType in providerTypes)
        {
            DiscoverMethods(providerType);
        }
    }

    /// <summary>
    /// Discovers node methods from a specific provider type.
    /// </summary>
    public void DiscoverMethods(Type providerType)
    {
        foreach (var method in providerType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = method.GetCustomAttribute<NodeMethodAttribute>();
            if (attr == null)
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Node method {providerType.Name}.{method.Name} must have at least one parameter (config type)");
            }

            var configType = parameters[0].ParameterType;
            var hasCancellationToken = parameters.Length > 1 &&
                                       parameters[1].ParameterType == typeof(CancellationToken);

            // Validate return type is Task<T>
            if (!method.ReturnType.IsGenericType ||
                method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
            {
                throw new InvalidOperationException(
                    $"Node method {providerType.Name}.{method.Name} must return Task<T>");
            }

            var outputType = method.ReturnType.GetGenericArguments()[0];

            _methods[attr.NodeType] = new NodeMethodInfo
            {
                NodeType = attr.NodeType,
                ProviderType = providerType,
                Method = method,
                ConfigType = configType,
                OutputType = outputType,
                HasCancellationToken = hasCancellationToken
            };
        }
    }

    /// <summary>
    /// Gets the method info for a specific node type.
    /// </summary>
    public NodeMethodInfo GetMethod(NodeType nodeType)
    {
        if (!_methods.TryGetValue(nodeType, out var methodInfo))
        {
            throw new ArgumentException($"No method registered for node type: {nodeType}", nameof(nodeType));
        }
        return methodInfo;
    }

    /// <summary>
    /// Checks if a method is registered for the specified node type.
    /// </summary>
    public bool HasMethod(NodeType nodeType)
    {
        return _methods.ContainsKey(nodeType);
    }
}
