using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using DonkeyWork.Agents.Scheduling.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Scheduling.Core.SystemJobs;

public sealed class CompletedOneOffCleanupHandler : ISystemJobHandler
{
    private readonly AgentsDbContext _dbContext;
    private readonly SchedulingServiceOptions _options;
    private readonly ILogger<CompletedOneOffCleanupHandler> _logger;

    public CompletedOneOffCleanupHandler(
        AgentsDbContext dbContext,
        SchedulingServiceOptions options,
        ILogger<CompletedOneOffCleanupHandler> logger)
    {
        _dbContext = dbContext;
        _options = options;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.CompletedOneOffRetentionDays);

        var deleted = await _dbContext.ScheduledJobs
            .IgnoreQueryFilters()
            .Where(j => j.ScheduleMode == ScheduleMode.OneOff
                        && !j.IsEnabled
                        && j.UpdatedAt < cutoff)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation("Cleaned up {Count} completed one-off schedules older than {Cutoff}", deleted, cutoff);
    }
}
