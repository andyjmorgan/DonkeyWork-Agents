using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Orchestrations;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

public class OrchestrationExecutionRepository : IOrchestrationExecutionRepository
{
    private readonly AgentsDbContext _dbContext;

    public OrchestrationExecutionRepository(AgentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetExecutionResponseV1> CreateAsync(
        Guid agentId,
        Guid versionId,
        string input,
        string streamName,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var execution = new OrchestrationExecutionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrchestrationId = agentId,
            OrchestrationVersionId = versionId,
            Status = ExecutionStatus.Running,
            Input = input,
            StartedAt = DateTimeOffset.UtcNow,
            StreamName = streamName,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.OrchestrationExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(execution);
    }

    public async Task UpdateCompletionAsync(
        Guid executionId,
        ExecutionStatus status,
        string? output,
        string? errorMessage,
        int? totalTokensUsed,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.OrchestrationExecutions
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution == null)
            throw new InvalidOperationException($"Execution {executionId} not found");

        execution.Status = status;
        execution.Output = output;
        execution.ErrorMessage = errorMessage;
        execution.TotalTokensUsed = totalTokensUsed;
        execution.CompletedAt = DateTimeOffset.UtcNow;
        execution.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GetExecutionResponseV1?> GetByIdAsync(
        Guid executionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.OrchestrationExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        return execution == null ? null : MapToResponse(execution);
    }

    public async Task<(IReadOnlyList<GetExecutionResponseV1> Executions, int TotalCount)> ListAsync(
        Guid? agentId,
        int offset,
        int limit,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.OrchestrationExecutions.AsNoTracking();

        if (agentId.HasValue)
        {
            query = query.Where(e => e.OrchestrationId == agentId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var executions = await query
            .OrderByDescending(e => e.StartedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (executions.Select(MapToResponse).ToList(), totalCount);
    }

    public async Task<(IReadOnlyList<ExecutionLogV1> Logs, int TotalCount)> GetLogsAsync(
        Guid executionId,
        int offset,
        int limit,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Verify user owns this execution
        var executionExists = await _dbContext.OrchestrationExecutions
            .AnyAsync(e => e.Id == executionId, cancellationToken);

        if (!executionExists)
        {
            return (Array.Empty<ExecutionLogV1>(), 0);
        }

        var query = _dbContext.OrchestrationExecutionLogs
            .AsNoTracking()
            .Where(l => l.ExecutionId == executionId);

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderBy(l => l.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (logs.Select(MapLogToResponse).ToList(), totalCount);
    }

    private static GetExecutionResponseV1 MapToResponse(OrchestrationExecutionEntity execution)
    {
        return new GetExecutionResponseV1
        {
            Id = execution.Id,
            OrchestrationId = execution.OrchestrationId,
            VersionId = execution.OrchestrationVersionId,
            Status = execution.Status,
            Input = JsonSerializer.Deserialize<JsonElement>(execution.Input),
            Output = execution.Output != null ? JsonSerializer.Deserialize<JsonElement>(execution.Output) : null,
            ErrorMessage = execution.ErrorMessage,
            StartedAt = execution.StartedAt,
            CompletedAt = execution.CompletedAt,
            TotalTokensUsed = execution.TotalTokensUsed
        };
    }

    private static ExecutionLogV1 MapLogToResponse(OrchestrationExecutionLogEntity log)
    {
        return new ExecutionLogV1
        {
            Id = log.Id,
            ExecutionId = log.ExecutionId,
            LogLevel = log.LogLevel,
            Message = log.Message,
            Details = log.Details,
            NodeId = log.NodeId,
            Source = log.Source,
            CreatedAt = log.CreatedAt.UtcDateTime
        };
    }

    public async Task<(IReadOnlyList<NodeExecutionV1> NodeExecutions, int TotalCount)> GetNodeExecutionsAsync(
        Guid executionId,
        int offset,
        int limit,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Verify user owns this execution
        var executionExists = await _dbContext.OrchestrationExecutions
            .AnyAsync(e => e.Id == executionId, cancellationToken);

        if (!executionExists)
        {
            return (Array.Empty<NodeExecutionV1>(), 0);
        }

        var query = _dbContext.OrchestrationNodeExecutions
            .AsNoTracking()
            .Where(n => n.OrchestrationExecutionId == executionId);

        var totalCount = await query.CountAsync(cancellationToken);

        var nodeExecutions = await query
            .OrderBy(n => n.StartedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (nodeExecutions.Select(MapNodeExecutionToResponse).ToList(), totalCount);
    }

    private static NodeExecutionV1 MapNodeExecutionToResponse(OrchestrationNodeExecutionEntity entity)
    {
        return new NodeExecutionV1
        {
            Id = entity.Id,
            NodeId = entity.NodeId,
            NodeType = entity.NodeType,
            NodeName = entity.NodeName,
            ActionType = entity.ActionType,
            Status = entity.Status,
            Input = entity.Input,
            Output = entity.Output,
            ErrorMessage = entity.ErrorMessage,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            DurationMs = entity.DurationMs,
            TokensUsed = entity.TokensUsed,
            FullResponse = entity.FullResponse
        };
    }
}
