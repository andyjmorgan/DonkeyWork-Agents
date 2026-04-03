using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using DonkeyWork.Agents.Scheduling.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Scheduling.Core.SystemJobs;

public sealed class ExecutionHistoryPruningHandler : ISystemJobHandler
{
    private readonly AgentsDbContext _dbContext;
    private readonly SchedulingServiceOptions _options;
    private readonly ILogger<ExecutionHistoryPruningHandler> _logger;

    public ExecutionHistoryPruningHandler(
        AgentsDbContext dbContext,
        SchedulingServiceOptions options,
        ILogger<ExecutionHistoryPruningHandler> logger)
    {
        _dbContext = dbContext;
        _options = options;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.ExecutionHistoryRetentionDays);

        var deleted = await _dbContext.ScheduledJobExecutions
            .IgnoreQueryFilters()
            .Where(e => e.StartedAtUtc < cutoff)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation("Pruned {Count} execution history records older than {Cutoff}", deleted, cutoff);
    }
}
