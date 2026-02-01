using System.Diagnostics;
using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Enums;
using DonkeyWork.Agents.Agents.Contracts.Models.Events;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using DonkeyWork.Agents.Agents.Core.Options;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Registry;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Agents.Core.Services;

/// <summary>
/// Orchestrates agent execution.
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly AgentsDbContext _dbContext;
    private readonly INodeExecutorRegistry _executorRegistry;
    private readonly IExecutionStreamWriter _streamWriter;
    private readonly IExecutionContext _executionContext;
    private readonly GraphAnalyzer _graphAnalyzer;
    private readonly IOptions<AgentsOptions> _options;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        AgentsDbContext dbContext,
        INodeExecutorRegistry executorRegistry,
        IExecutionStreamWriter streamWriter,
        IExecutionContext executionContext,
        GraphAnalyzer graphAnalyzer,
        IOptions<AgentsOptions> options,
        ILogger<AgentOrchestrator> logger)
    {
        _dbContext = dbContext;
        _executorRegistry = executorRegistry;
        _streamWriter = streamWriter;
        _executionContext = executionContext;
        _graphAnalyzer = graphAnalyzer;
        _options = options;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        Guid executionId,
        Guid userId,
        Guid versionId,
        object input,
        CancellationToken cancellationToken = default)
    {
        // Create timeout token source
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.Value.ExecutionTimeout);

        try
        {
            // Load agent version
            var version = await _dbContext.AgentVersions
                .Include(v => v.Agent)
                .FirstOrDefaultAsync(v => v.Id == versionId, timeoutCts.Token);

            if (version == null)
            {
                throw new InvalidOperationException($"Agent version not found: {versionId}");
            }

            // 1. Create AgentExecution record (Status: Pending)
            var execution = new AgentExecutionEntity
            {
                Id = executionId,
                UserId = userId,
                AgentId = version.AgentId,
                AgentVersionId = versionId,
                Status = ExecutionStatus.Pending.ToString(),
                Input = JsonSerializer.Serialize(input),
                StartedAt = DateTimeOffset.UtcNow,
                StreamName = $"execution-{executionId}"
            };

            _dbContext.AgentExecutions.Add(execution);
            await _dbContext.SaveChangesAsync(timeoutCts.Token);

            // 2. Initialize the stream writer and emit ExecutionStarted event
            await _streamWriter.InitializeAsync(executionId);
            await _streamWriter.WriteEventAsync(new ExecutionStartedEvent());

            // 3. Set Status: Running
            execution.Status = ExecutionStatus.Running.ToString();
            await _dbContext.SaveChangesAsync(timeoutCts.Token);

            // 4. Analyze graph (topological sort)
            var reactFlowData = JsonSerializer.Deserialize<JsonElement>(version.ReactFlowData);
            var analysisResult = _graphAnalyzer.Analyze(reactFlowData);

            if (!analysisResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"Graph analysis failed: {analysisResult.ErrorMessage}");
            }

            // Parse node configurations
            var nodeConfigsJson = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                version.NodeConfigurations);

            if (nodeConfigsJson == null)
            {
                throw new InvalidOperationException("Failed to parse node configurations");
            }

            // 5. Hydrate execution context
            _executionContext.Hydrate(executionId, userId, input, version.InputSchema);

            // 6. For each node in execution order
            var executionStopwatch = Stopwatch.StartNew();

            foreach (var nodeId in analysisResult.ExecutionOrder)
            {
                // Get node from ReactFlow data
                var reactFlowNodes = reactFlowData.GetProperty("nodes").EnumerateArray();
                var node = reactFlowNodes.FirstOrDefault(n => n.GetProperty("id").GetString() == nodeId);

                if (node.ValueKind == JsonValueKind.Undefined)
                {
                    throw new InvalidOperationException($"Node not found in ReactFlow data: {nodeId}");
                }

                var nodeType = node.GetProperty("type").GetString();
                if (string.IsNullOrEmpty(nodeType))
                {
                    throw new InvalidOperationException($"Node missing type: {nodeId}");
                }

                // Get node configuration
                if (!nodeConfigsJson.TryGetValue(nodeId, out var nodeConfigElement))
                {
                    throw new InvalidOperationException($"Node configuration not found: {nodeId}");
                }

                // Log the raw config for debugging
                _logger.LogDebug(
                    "Node {NodeId} config JSON: {ConfigJson}",
                    nodeId,
                    nodeConfigElement.GetRawText());

                // Deserialize node configuration using polymorphic deserialization
                // Inject type discriminator from ReactFlow to support both old data (without discriminator) and new data
                var nodeConfig = DeserializeNodeConfiguration(nodeConfigElement, nodeType, _logger)
                    ?? throw new InvalidOperationException($"Failed to parse node config: {nodeId}");

                // Get node name from configuration (all NodeConfiguration subclasses have Name)
                var nodeName = nodeConfig.Name;

                // Convert string to enum for type-safe comparisons
                var nodeTypeEnum = nodeType.ToNodeType();

                // Determine input for this node
                string? nodeInput = null;
                if (nodeTypeEnum == NodeType.Start)
                {
                    // Start node input is the execution input
                    nodeInput = JsonSerializer.Serialize(input);
                }
                else
                {
                    // Other nodes get their input from upstream node outputs
                    var upstreamOutputs = _executionContext.NodeOutputs;
                    if (upstreamOutputs.Count > 0)
                    {
                        nodeInput = JsonSerializer.Serialize(upstreamOutputs);
                    }
                }

                // Create NodeExecution record
                var nodeExecution = new AgentNodeExecutionEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AgentExecutionId = executionId,
                    NodeId = nodeId,
                    NodeType = nodeType,
                    NodeName = nodeName,
                    Status = ExecutionStatus.Running.ToString(),
                    Input = nodeInput,
                    StartedAt = DateTimeOffset.UtcNow
                };

                _dbContext.AgentNodeExecutions.Add(nodeExecution);
                await _dbContext.SaveChangesAsync(timeoutCts.Token);

                try
                {
                    // Get executor from registry (nodeTypeEnum is already converted above)
                    var executor = _executorRegistry.GetExecutor(nodeTypeEnum) as INodeExecutor;
                    if (executor == null)
                    {
                        throw new InvalidOperationException($"Executor not found for node type: {nodeType}");
                    }

                    // Execute node (timed) - executor emits NodeStarted/NodeCompleted events
                    var nodeStopwatch = Stopwatch.StartNew();
                    var output = await executor.ExecuteAsync(
                        nodeId,
                        nodeConfig,
                        timeoutCts.Token);
                    nodeStopwatch.Stop();

                    // Store output in context using node name
                    _executionContext.SetNodeOutput(nodeName, output);

                    // Update NodeExecution record
                    nodeExecution.Status = ExecutionStatus.Completed.ToString();
                    nodeExecution.CompletedAt = DateTimeOffset.UtcNow;
                    nodeExecution.DurationMs = (int)nodeStopwatch.ElapsedMilliseconds;
                    nodeExecution.Output = JsonSerializer.Serialize(output);

                    // For model nodes, store additional information
                    if (output is ModelNodeOutput modelOutput)
                    {
                        nodeExecution.TokensUsed = modelOutput.TotalTokens;
                        nodeExecution.FullResponse = modelOutput.ResponseText;
                    }

                    await _dbContext.SaveChangesAsync(timeoutCts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Node execution failed: {NodeId} ({NodeType})", nodeId, nodeType);

                    // Update node execution as failed
                    nodeExecution.Status = ExecutionStatus.Failed.ToString();
                    nodeExecution.CompletedAt = DateTimeOffset.UtcNow;
                    nodeExecution.ErrorMessage = ex.Message;
                    await _dbContext.SaveChangesAsync(CancellationToken.None);

                    // Re-throw to fail the entire execution
                    throw;
                }
            }

            executionStopwatch.Stop();

            // 7. Write final output to AgentExecution
            // The final output is from the end node
            var endNodeOutput = _executionContext.NodeOutputs.Values
                .OfType<EndNodeOutput>()
                .FirstOrDefault();

            if (endNodeOutput != null)
            {
                execution.Output = JsonSerializer.Serialize(endNodeOutput.FinalOutput);
            }

            // Calculate total tokens
            var totalTokens = _executionContext.NodeOutputs.Values
                .OfType<ModelNodeOutput>()
                .Sum(o => o.TotalTokens ?? 0);

            execution.TotalTokensUsed = totalTokens > 0 ? totalTokens : null;

            // 8. Set Status: Completed
            execution.Status = ExecutionStatus.Completed.ToString();
            execution.CompletedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            // 9. Emit ExecutionCompleted event
            await _streamWriter.WriteEventAsync(
                new ExecutionCompletedEvent
                {
                    Output = execution.Output ?? string.Empty
                });
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred
            _logger.LogWarning("Execution timeout: {ExecutionId}", executionId);

            await HandleExecutionFailureAsync(
                executionId,
                "Execution timeout",
                CancellationToken.None);

            throw new TimeoutException("Execution timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execution failed: {ExecutionId}", executionId);

            await HandleExecutionFailureAsync(
                executionId,
                ex.Message,
                CancellationToken.None);
        }
    }

    private async Task HandleExecutionFailureAsync(
        Guid executionId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var execution = await _dbContext.AgentExecutions
                .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

            if (execution != null)
            {
                execution.Status = ExecutionStatus.Failed.ToString();
                execution.CompletedAt = DateTimeOffset.UtcNow;
                execution.ErrorMessage = errorMessage;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await _streamWriter.WriteEventAsync(
                new ExecutionFailedEvent
                {
                    ErrorMessage = errorMessage
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle execution failure for {ExecutionId}", executionId);
        }
    }

    /// <summary>
    /// Deserializes a node configuration using the NodeConfigurationRegistry.
    /// The stored JSON includes type discriminators added during save.
    /// </summary>
    private static NodeConfiguration? DeserializeNodeConfiguration(
        JsonElement configElement,
        string nodeType,
        ILogger logger)
    {
        var registry = NodeConfigurationRegistry.Instance;

        // Check if type discriminator is already present (new data)
        if (configElement.TryGetProperty("type", out var existingType))
        {
            logger.LogDebug(
                "Type discriminator present: {Type}",
                existingType.GetString());
            return JsonSerializer.Deserialize<NodeConfiguration>(
                configElement.GetRawText(),
                registry.JsonOptions);
        }

        // Inject type discriminator for backwards compatibility with old data
        // Map ReactFlow node type to discriminator format
        var discriminator = MapNodeTypeToDiscriminator(nodeType);

        logger.LogDebug(
            "Injecting type discriminator '{Discriminator}' for node type '{NodeType}'",
            discriminator,
            nodeType);

        var configWithType = new Dictionary<string, object>
        {
            ["type"] = discriminator
        };

        foreach (var property in configElement.EnumerateObject())
        {
            configWithType[property.Name] = property.Value;
        }

        var configJson = JsonSerializer.Serialize(configWithType, registry.JsonOptions);
        logger.LogDebug("Enriched config JSON: {ConfigJson}", configJson);

        return JsonSerializer.Deserialize<NodeConfiguration>(configJson, registry.JsonOptions);
    }

    /// <summary>
    /// Maps ReactFlow node type (lowercase) to the polymorphic type discriminator.
    /// </summary>
    private static string MapNodeTypeToDiscriminator(string reactFlowType)
    {
        return reactFlowType switch
        {
            "start" => "Start",
            "end" => "End",
            "model" => "Model",
            "messageFormatter" => "MessageFormatter",
            "httpRequest" => "HttpRequest",
            "sleep" => "Sleep",
            _ => throw new InvalidOperationException($"Unknown node type: {reactFlowType}")
        };
    }
}
