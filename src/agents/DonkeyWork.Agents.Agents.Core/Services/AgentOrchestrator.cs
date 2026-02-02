using System.Diagnostics;
using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Enums;
using DonkeyWork.Agents.Agents.Contracts.Models.Events;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using DonkeyWork.Agents.Agents.Core.Options;
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
                Status = ExecutionStatus.Pending,
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
            execution.Status = ExecutionStatus.Running;
            await _dbContext.SaveChangesAsync(timeoutCts.Token);

            // 4. Analyze graph (topological sort) - use typed ReactFlowData directly
            var analysisResult = _graphAnalyzer.Analyze(version.ReactFlowData);

            if (!analysisResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"Graph analysis failed: {analysisResult.ErrorMessage}");
            }

            // NodeConfigurations is already a typed Dictionary<string, NodeConfiguration>
            var nodeConfigurations = version.NodeConfigurations;

            // 5. Hydrate execution context
            _executionContext.Hydrate(executionId, userId, input, version.InputSchema);

            // 6. For each node in execution order
            var executionStopwatch = Stopwatch.StartNew();

            foreach (var nodeId in analysisResult.ExecutionOrder)
            {
                // Get node from typed ReactFlow data
                var node = version.ReactFlowData.Nodes.FirstOrDefault(n => n.Id == nodeId);

                if (node == null)
                {
                    throw new InvalidOperationException($"Node not found in ReactFlow data: {nodeId}");
                }

                // Use typed NodeType enum directly
                var nodeTypeEnum = node.Data.NodeType;

                // Get node configuration - already typed
                if (!nodeConfigurations.TryGetValue(nodeId, out var nodeConfig))
                {
                    throw new InvalidOperationException($"Node configuration not found: {nodeId}");
                }

                // Log for debugging
                _logger.LogDebug(
                    "Node {NodeId} type: {NodeType}, config type: {ConfigType}",
                    nodeId,
                    nodeTypeEnum,
                    nodeConfig.GetType().Name);

                // Get node name from configuration (all NodeConfiguration subclasses have Name)
                var nodeName = nodeConfig.Name;

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
                    NodeType = nodeTypeEnum,
                    NodeName = nodeName,
                    Status = ExecutionStatus.Running,
                    Input = nodeInput,
                    StartedAt = DateTimeOffset.UtcNow
                };

                _dbContext.AgentNodeExecutions.Add(nodeExecution);
                await _dbContext.SaveChangesAsync(timeoutCts.Token);

                try
                {
                    // Get executor from registry
                    var executor = _executorRegistry.GetExecutor(nodeTypeEnum) as INodeExecutor;
                    if (executor == null)
                    {
                        throw new InvalidOperationException($"Executor not found for node type: {nodeTypeEnum}");
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
                    nodeExecution.Status = ExecutionStatus.Completed;
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
                    _logger.LogError(ex, "Node execution failed: {NodeId} ({NodeType})", nodeId, nodeTypeEnum);

                    // Update node execution as failed
                    nodeExecution.Status = ExecutionStatus.Failed;
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
            execution.Status = ExecutionStatus.Completed;
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
                execution.Status = ExecutionStatus.Failed;
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

}
