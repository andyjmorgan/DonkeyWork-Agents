using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Scheduling.Api.Options;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Models;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using DonkeyWork.Agents.Scheduling.Core.Quartz;
using DonkeyWork.Agents.Scheduling.Core.Services;
using DonkeyWork.Agents.Scheduling.Core.SystemJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl.Matchers;

namespace DonkeyWork.Agents.Scheduling.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulingApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var schedulingOptions = configuration
            .GetSection(Options.SchedulingOptions.SectionName)
            .Get<Options.SchedulingOptions>() ?? new Options.SchedulingOptions();

        services.AddOptions<Options.SchedulingOptions>()
            .BindConfiguration(Options.SchedulingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var persistenceOptions = configuration
            .GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>() ?? new PersistenceOptions();
        var persistenceConnectionString = persistenceOptions.ConnectionString;

        services.AddScoped<IScheduledJobRepository, ScheduledJobRepository>();
        services.AddScoped<IScheduledJobExecutionRepository, ScheduledJobExecutionRepository>();

        services.AddSingleton(new SchedulingServiceOptions
        {
            DefaultTimeZone = schedulingOptions.DefaultTimeZone,
            MinimumCronIntervalHours = schedulingOptions.MinimumCronIntervalHours,
            ExecutionHistoryRetentionDays = schedulingOptions.ExecutionHistoryRetentionDays,
            CompletedOneOffRetentionDays = schedulingOptions.CompletedOneOffRetentionDays,
        });
        services.AddScoped<ISchedulingService, SchedulingService>();

        services.AddSingleton<ScheduledJobListener>();

        services.AddScoped<ExecutionHistoryPruningHandler>();
        services.AddScoped<CompletedOneOffCleanupHandler>();

        services.AddSingleton<SystemJobDefinition>(new SystemJobDefinition
        {
            Name = "Execution History Pruning",
            CronExpression = "0 0 3 * * ?",
            JobType = ScheduleJobType.Cleanup,
            HandlerType = typeof(ExecutionHistoryPruningHandler),
        });
        services.AddSingleton<SystemJobDefinition>(new SystemJobDefinition
        {
            Name = "Completed One-Off Cleanup",
            CronExpression = "0 0 4 * * ?",
            JobType = ScheduleJobType.Cleanup,
            HandlerType = typeof(CompletedOneOffCleanupHandler),
        });

        services.AddQuartz(q =>
        {
            q.SchedulerId = "AUTO";
            q.SchedulerName = "DonkeyWorkScheduler";

            if (!string.IsNullOrEmpty(persistenceConnectionString))
            {
                q.UsePersistentStore(store =>
                {
                    store.UsePostgres(pg =>
                    {
                        pg.ConnectionString = persistenceConnectionString;
                        pg.TablePrefix = "scheduling.qrtz_";
                    });

                    store.UseSystemTextJsonSerializer();
                    store.UseProperties = true;

                    store.UseClustering(cluster =>
                    {
                        cluster.CheckinInterval = TimeSpan.FromSeconds(15);
                        cluster.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
                    });
                });
            }

            q.AddJobListener<ScheduledJobListener>(GroupMatcher<JobKey>.AnyGroup());
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        services.AddHostedService<ScheduleReconciliationService>();
        services.AddHostedService<SystemJobRegistrar>();

        return services;
    }
}
