using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DonkeyWork.Agents.Scheduling.Core.Quartz;

[DisallowConcurrentExecution]
public class SystemTaskJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AgentsDbContext _dbContext;
    private readonly ILogger<SystemTaskJob> _logger;

    public SystemTaskJob(
        IServiceProvider serviceProvider,
        AgentsDbContext dbContext,
        ILogger<SystemTaskJob> logger)
    {
        _serviceProvider = serviceProvider;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var scheduleIdStr = context.MergedJobDataMap.GetString(ScheduleDataMapKeys.ScheduleId);
        if (!Guid.TryParse(scheduleIdStr, out var scheduleId))
        {
            _logger.LogError("SystemTaskJob fired without valid ScheduleId");
            return;
        }

        var schedule = await _dbContext.ScheduledJobs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(j => j.Id == scheduleId, context.CancellationToken);

        if (schedule is null)
        {
            _logger.LogError("SystemTaskJob fired for non-existent schedule {ScheduleId}", scheduleId);
            return;
        }

        var handlerTypeName = context.MergedJobDataMap.GetString(ScheduleDataMapKeys.SystemHandlerType);
        if (string.IsNullOrEmpty(handlerTypeName))
        {
            _logger.LogError("SystemTaskJob for schedule {ScheduleId} has no handler type", scheduleId);
            return;
        }

        var handlerType = Type.GetType(handlerTypeName);
        if (handlerType is null)
        {
            _logger.LogError("SystemTaskJob handler type {HandlerType} not found", handlerTypeName);
            return;
        }

        var handler = _serviceProvider.GetService(handlerType) as ISystemJobHandler;
        if (handler is null)
        {
            _logger.LogError("SystemTaskJob handler {HandlerType} not registered in DI", handlerTypeName);
            return;
        }

        _logger.LogInformation("Executing system job {Name} ({ScheduleId})", schedule.Name, scheduleId);

        try
        {
            await handler.ExecuteAsync(context.CancellationToken);
            _logger.LogInformation("System job {Name} completed", schedule.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System job {Name} failed", schedule.Name);
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}
