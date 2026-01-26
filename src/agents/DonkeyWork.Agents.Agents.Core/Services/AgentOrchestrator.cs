using System.Diagnostics;
using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Enums;
using DonkeyWork.Agents.Agents.Contracts.Models.Events;
using DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Agents.Core.Execution;
using DonkeyWork.Agents.Agents.Core.Execution.Outputs;
using DonkeyWork.Agents.Agents.Core.Options;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ExecutionContext = DonkeyWork.Agents.Agents.Core.Execution.ExecutionContext;

namespace DonkeyWork.Agents.Agents.Core.Services;

/// <summary>
/// Orchestrates agent execution.
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly AgentsDbContext _dbContext;
    private readonly INodeExecutorRegistry _executorRegistry;
    private readonly IExecutionStreamService _streamService;
    private readonly GraphAnalyzer _graphAnalyzer;
    private readonly IOptions<AgentsOptions> _options;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        AgentsDbContext dbContext,
        INodeExecutorRegistry executorRegistry,
        IExecutionStreamService streamService,
        GraphAnalyzer graphAnalyzer,
        IOptions<AgentsOptions> options,
        ILogger<AgentOrchestrator> logger)
    {
        _dbContext = dbContext;
        _executorRegistry = executorRegistry;
        _streamService = streamService;
        _graphAnalyzer = graphAnalyzer;
        _options = options;
        _logger = logger;
    }

    public async Task<Guid> ExecuteAsync(
        Guid userId,
        Guid versionId,
        object input,
        CancellationToken cancellationToken = default)
    {
        // Create timeout token source
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_options.Value.ExecutionTimeout);

        Guid executionId = Guid.NewGuid();

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

            // 2. Create RabbitMQ Stream
            await _streamService.CreateStreamAsync(executionId);

            // 3. Emit ExecutionStarted event
            await _streamService.WriteEventAsync(
                executionId,
                new ExecutionStartedEvent());

            // 4. Set Status: Running
            execution.Status = ExecutionStatus.Running.ToString();
            await _dbContext.SaveChangesAsync(timeoutCts.Token);

            // 5. Analyze graph (topological sort)
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

            // 6. Create ExecutionContext
            var context = new ExecutionContext
            {
                ExecutionId = executionId,
                UserId = userId,
                Input = input,
                InputSchema = version.InputSchema
            };

            // 7. For each node in execution order
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

                // Deserialize node configuration based on type
                object nodeConfig = nodeType switch
                {
                    "start" => JsonSerializer.Deserialize<StartNodeConfiguration>(nodeConfigElement.GetRawText())
                        ?? throw new InvalidOperationException($"Failed to parse start node config: {nodeId}"),
                    "model" => JsonSerializer.Deserialize<ModelNodeConfiguration>(nodeConfigElement.GetRawText())
                        ?? throw new InvalidOperationException($"Failed to parse model node config: {nodeId}"),
                    "end" => JsonSerializer.Deserialize<EndNodeConfiguration>(nodeConfigElement.GetRawText())
                        ?? throw new InvalidOperationException($"Failed to parse end node config: {nodeId}"),
                    _ => throw new InvalidOperationException($"Unsupported node type: {nodeType}")
                };

                // Get node name from configuration
                var nodeName = nodeConfig switch
                {
                    StartNodeConfiguration startConfig => startConfig.Name,
                    ModelNodeConfiguration modelConfig => modelConfig.Name,
                    EndNodeConfiguration endConfig => endConfig.Name,
                    _ => throw new InvalidOperationException($"Unknown config type for node: {nodeId}")
                };

                // Create NodeExecution record
                var nodeExecution = new AgentNodeExecutionEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AgentExecutionId = executionId,
                    NodeId = nodeId,
                    NodeType = nodeType,
                    Status = ExecutionStatus.Running.ToString(),
                    StartedAt = DateTimeOffset.UtcNow
                };

                _dbContext.AgentNodeExecutions.Add(nodeExecution);
                await _dbContext.SaveChangesAsync(timeoutCts.Token);

                // Emit NodeStarted event
                await _streamService.WriteEventAsync(
                    executionId,
                    new NodeStartedEvent
                    {
                        NodeId = nodeId
                    });

                try
                {
                    // Get executor from registry
                    var executor = _executorRegistry.GetExecutor(nodeType) as INodeExecutor;
                    if (executor == null)
                    {
                        throw new InvalidOperationException($"Executor not found for node type: {nodeType}");
                    }

                    // Execute node (timed)
                    var nodeStopwatch = Stopwatch.StartNew();
                    var output = await executor.ExecuteAsync(
                        nodeConfig,
                        context,
                        _streamService,
                        timeoutCts.Token);
                    nodeStopwatch.Stop();

                    // Store output in context using node name
                    context.NodeOutputs[nodeName] = output;

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

                    // Emit NodeCompleted event
                    await _streamService.WriteEventAsync(
                        executionId,
                        new NodeCompletedEvent
                        {
                            NodeId = nodeId,
                            Output = nodeExecution.Output ?? string.Empty
                        });
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

            // 8. Write final output to AgentExecution
            // The final output is from the end node
            var endNodeOutput = context.NodeOutputs.Values
                .OfType<EndNodeOutput>()
                .FirstOrDefault();

            if (endNodeOutput != null)
            {
                execution.Output = JsonSerializer.Serialize(endNodeOutput.FinalOutput);
            }

            // Calculate total tokens
            var totalTokens = context.NodeOutputs.Values
                .OfType<ModelNodeOutput>()
                .Sum(o => o.TotalTokens ?? 0);

            execution.TotalTokensUsed = totalTokens > 0 ? totalTokens : null;

            // 9. Set Status: Completed
            execution.Status = ExecutionStatus.Completed.ToString();
            execution.CompletedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            // 10. Emit ExecutionCompleted event
            await _streamService.WriteEventAsync(
                executionId,
                new ExecutionCompletedEvent
                {
                    Output = execution.Output ?? string.Empty
                });

            return executionId;
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

            // Never throw - always return execution ID
            return executionId;
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

            await _streamService.WriteEventAsync(
                executionId,
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
