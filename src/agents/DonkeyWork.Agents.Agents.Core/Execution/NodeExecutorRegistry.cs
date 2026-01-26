using DonkeyWork.Agents.Agents.Contracts.Services;

namespace DonkeyWork.Agents.Agents.Core.Execution;

/// <summary>
/// Registry for node executors.
/// </summary>
public class NodeExecutorRegistry : INodeExecutorRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _executorTypes = new(StringComparer.OrdinalIgnoreCase);

    public NodeExecutorRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Registers a node executor type.
    /// </summary>
    /// <param name="nodeType">The node type string (e.g., "start", "model", "end").</param>
    /// <param name="executorType">The executor type.</param>
    public void Register(string nodeType, Type executorType)
    {
        if (!typeof(INodeExecutor).IsAssignableFrom(executorType))
        {
            throw new ArgumentException(
                $"Executor type must implement INodeExecutor: {executorType.Name}",
                nameof(executorType));
        }

        _executorTypes[nodeType] = executorType;
    }

    /// <inheritdoc/>
    public object GetExecutor(string nodeType)
    {
        if (!_executorTypes.TryGetValue(nodeType, out var executorType))
        {
            throw new InvalidOperationException($"No executor registered for node type: {nodeType}");
        }

        var executor = _serviceProvider.GetService(executorType);
        if (executor == null)
        {
            throw new InvalidOperationException(
                $"Executor type {executorType.Name} is not registered in DI container");
        }

        return executor;
    }
}
