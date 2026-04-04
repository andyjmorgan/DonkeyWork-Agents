using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Scheduling.Core.SystemJobs;

public sealed class StaleAgentExecutionReaperHandler : ISystemJobHandler
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(6);

    private readonly AgentsDbContext _dbContext;
    private readonly ILogger<StaleAgentExecutionReaperHandler> _logger;

    public StaleAgentExecutionReaperHandler(
        AgentsDbContext dbContext,
        ILogger<StaleAgentExecutionReaperHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow - StaleThreshold;

        var updated = await _dbContext.AgentExecutions
            .IgnoreQueryFilters()
            .Where(e => (e.Status == "Running" || e.Status == "Idle") && e.StartedAt < cutoff)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, "Stale")
                .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow), ct);

        if (updated > 0)
            _logger.LogInformation("Marked {Count} stale agent executions (no update for {Threshold})", updated, StaleThreshold);
    }
}
