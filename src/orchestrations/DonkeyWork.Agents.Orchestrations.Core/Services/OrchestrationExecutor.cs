using System.Diagnostics;
using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Core.Execution;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;
using DonkeyWork.Agents.Orchestrations.Core.Options;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Orchestrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

/// <summary>
/// Orchestrates agent execution.
/// </summary>
public class OrchestrationExecutor : IOrchestrationExecutor
{
    private readonly AgentsDbContext _dbContext;
    private readonly INodeExecutorRegistry _executorRegistry;
    private readonly IExecutionStreamWriter _streamWriter;
    private readonly IExecutionContext _executionContext;
    private readonly GraphAnalyzer _graphAnalyzer;
    private readonly IOptions<OrchestrationsOptions> _options;
    private readonly ILogger<OrchestrationExecutor> _logger;

    public OrchestrationExecutor(
        AgentsDbContext dbContext,
        INodeExecutorRegistry executorRegistry,
        IExecutionStreamWriter streamWriter,
        IExecutionContext executionContext,
        GraphAnalyzer graphAnalyzer,
        IOptions<OrchestrationsOptions> options,
        ILogger<OrchestrationExecutor> logger)
    {
        _dbContext = dbContext;
        _executorRegistry = executorRegistry;
        _streamWriter = streamWriter;
        _executionContext = executionContext;
        _graphAnalyzer = graphAnalyzer;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(
        Guid executionId,
        Guid userId,
        Guid versionId,
        ExecutionInterface executionInterface,
        JsonElement input,
        CancellationToken cancellationToken = default)
    {
        await ExecuteCoreAsync(
            executionId,
            userId,
            versionId,
            executionInterface,
            input,
            conversation: null,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task ExecuteChatAsync(
        Guid executionId,
        Guid userId,
        Guid versionId,
        ConversationContext conversation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteCoreAsync(
            executionId,
            userId,
            versionId,
            ExecutionInterface.Chat,
            input: default,
            conversation,
            cancellationToken);
    }

    private async Task ExecuteCoreAsync(
        Guid executionId,
        Guid userId,
        Guid versionId,
        ExecutionInterface executionInterface,
        JsonElement input,
        ConversationContext? conversation,
        CancellationToken cancellationToken)
    {
        var version = await _dbContext.OrchestrationVersions
            .Include(v => v.Orchestration)
            .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken);

        if (version == null)
        {
            throw new InvalidOperationException($"Orchestration version not found: {versionId}");
        }

        var effectiveTimeout = version.ExecutionTimeoutSeconds.HasValue
            ? TimeSpan.FromSeconds(version.ExecutionTimeoutSeconds.Value)
            : _options.Value.ExecutionTimeout;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(effectiveTimeout);

        try
        {
            // Load existing execution record (pre-created by controller via pub/sub path)
            // or create one for direct callers (e.g., OrchestrationToolProvider from Orleans grains)
            var execution = await _dbContext.OrchestrationExecutions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == executionId, timeoutCts.Token);

            if (execution == null)
            {
                var inputJson = conversation != null
                    ? JsonSerializer.Serialize(new { conversationId = conversation.Id })
                    : input.GetRawText();

                execution = new OrchestrationExecutionEntity
                {
                    Id = executionId,
                    UserId = userId,
                    OrchestrationId = version.OrchestrationId,
                    OrchestrationVersionId = versionId,
                    Interface = executionInterface,
                    Status = ExecutionStatus.Pending,
                    Input = inputJson,
                    StartedAt = DateTimeOffset.UtcNow,
                    StreamName = NatsSubjects.ExecutionSubject(userId, executionId)
                };

                _dbContext.OrchestrationExecutions.Add(execution);
                await _dbContext.SaveChangesAsync(timeoutCts.Token);
            }

            // Initialize the stream writer and emit ExecutionStarted event
            await _streamWriter.InitializeAsync(userId, executionId);
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
            if (conversation != null)
            {
                _executionContext.HydrateChat(executionId, userId, conversation, version.InputSchema);
            }
            else
            {
                _executionContext.Hydrate(executionId, userId, executionInterface, input, version.InputSchema);
            }

            // 6. For each node in execution order
            var executionStopwatch = Stopwatch.StartNew();

            foreach (var nodeId in analysisResult.ExecutionOrder)
            {
                var node = version.ReactFlowData.Nodes.FirstOrDefault(n => n.Id == nodeId);

                if (node == null)
                {
                    throw new InvalidOperationException($"Node not found in ReactFlow data: {nodeId}");
                }

                // Use typed NodeType enum directly
                var nodeTypeEnum = node.Data.NodeType;

                if (!nodeConfigurations.TryGetValue(nodeId, out var nodeConfig))
                {
                    throw new InvalidOperationException($"Node configuration not found: {nodeId}");
                }

                _logger.LogDebug(
                    "Node {NodeId} type: {NodeType}, config type: {ConfigType}",
                    nodeId,
                    nodeTypeEnum,
                    nodeConfig.GetType().Name);

                var nodeName = nodeConfig.Name;

                // Determine input for this node
                string? nodeInput = null;
                if (nodeTypeEnum == NodeType.Start)
                {
                    nodeInput = execution.Input;
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

                var nodeExecution = new OrchestrationNodeExecutionEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    OrchestrationExecutionId = executionId,
                    NodeId = nodeId,
                    NodeType = nodeTypeEnum,
                    NodeName = nodeName,
                    Status = ExecutionStatus.Running,
                    Input = nodeInput,
                    StartedAt = DateTimeOffset.UtcNow
                };

                _dbContext.OrchestrationNodeExecutions.Add(nodeExecution);
                await _dbContext.SaveChangesAsync(timeoutCts.Token);

                try
                {
                    var executor = _executorRegistry.GetExecutor(nodeTypeEnum) as INodeExecutor;
                    if (executor == null)
                    {
                        throw new InvalidOperationException($"Executor not found for node type: {nodeTypeEnum}");
                    }

                    var nodeStopwatch = Stopwatch.StartNew();
                    var output = await executor.ExecuteAsync(
                        nodeId,
                        nodeConfig,
                        timeoutCts.Token);
                    nodeStopwatch.Stop();

                    _executionContext.SetNodeOutput(nodeName, output);

                    nodeExecution.Status = ExecutionStatus.Completed;
                    nodeExecution.CompletedAt = DateTimeOffset.UtcNow;
                    nodeExecution.DurationMs = (int)nodeStopwatch.ElapsedMilliseconds;
                    nodeExecution.Output = JsonSerializer.Serialize(output, output.GetType());

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

            var messageOutput = endNodeOutput?.ToMessageOutput() ?? string.Empty;

            if (endNodeOutput != null)
            {
                // For DB storage in jsonb column, wrap string in JSON
                execution.Output = JsonSerializer.Serialize(messageOutput);
            }

            var totalTokens = _executionContext.NodeOutputs.Values
                .OfType<ModelNodeOutput>()
                .Sum(o => o.TotalTokens ?? 0);

            execution.TotalTokensUsed = totalTokens > 0 ? totalTokens : null;

            // 8. Set Status: Completed
            execution.Status = ExecutionStatus.Completed;
            execution.CompletedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            // 9. Emit ExecutionCompleted event with raw message (not JSON-wrapped)
            await _streamWriter.WriteEventAsync(
                new ExecutionCompletedEvent
                {
                    Output = messageOutput
                });
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
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
            var execution = await _dbContext.OrchestrationExecutions
                .IgnoreQueryFilters()
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
