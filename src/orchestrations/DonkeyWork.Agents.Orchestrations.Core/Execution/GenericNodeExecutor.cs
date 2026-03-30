using System.Diagnostics;
using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Providers;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution;

/// <summary>
/// Generic node executor that routes execution to provider methods.
/// Used for simple nodes that don't need dedicated executor classes.
/// </summary>
public class GenericNodeExecutor : INodeExecutor
{
    private readonly NodeMethodRegistry _methodRegistry;
    private readonly IServiceProvider _serviceProvider;
    private readonly IExecutionStreamWriter _streamWriter;

    public GenericNodeExecutor(
        NodeMethodRegistry methodRegistry,
        IServiceProvider serviceProvider,
        IExecutionStreamWriter streamWriter)
    {
        _methodRegistry = methodRegistry;
        _serviceProvider = serviceProvider;
        _streamWriter = streamWriter;
    }

    public async Task<NodeOutput> ExecuteAsync(
        Guid nodeId,
        object config,
        CancellationToken cancellationToken)
    {
        if (config is not NodeConfiguration nodeConfig)
        {
            throw new InvalidOperationException(
                $"Config must be a NodeConfiguration, got {config.GetType().Name}");
        }

        // Look up provider method by NodeType
        var methodInfo = _methodRegistry.GetMethod(nodeConfig.NodeType);

        var provider = _serviceProvider.GetService(methodInfo.ProviderType)
            ?? throw new InvalidOperationException(
                $"Provider {methodInfo.ProviderType.Name} not registered in DI");

        await _streamWriter.WriteEventAsync(new NodeStartedEvent { NodeId = nodeId });

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var parameters = new List<object?> { config };
            if (methodInfo.HasCancellationToken)
            {
                parameters.Add(cancellationToken);
            }

            // Invoke provider method
            var task = (Task)methodInfo.Method.Invoke(provider, parameters.ToArray())!;
            await task;

            stopwatch.Stop();

            var resultProperty = task.GetType().GetProperty("Result")!;
            var output = (NodeOutput)resultProperty.GetValue(task)!;

            await _streamWriter.WriteEventAsync(
                new NodeCompletedEvent
                {
                    NodeId = nodeId,
                    Output = JsonSerializer.Serialize(output)
                });

            return output;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var innerEx = ex.InnerException ?? ex;
            throw new InvalidOperationException(
                $"Node execution failed: {innerEx.Message}", innerEx);
        }
    }
}
