using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Scheduling.Api.Options;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using DonkeyWork.Agents.Scheduling.Core.Quartz;
using DonkeyWork.Agents.Scheduling.Core.Services;
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

        var persistenceConnectionString = configuration
            .GetSection(PersistenceOptions.SectionName)
            .Get<PersistenceOptions>()?.ConnectionString
            ?? throw new InvalidOperationException("Persistence connection string is required for scheduling.");

        services.AddScoped<IScheduledJobRepository, ScheduledJobRepository>();
        services.AddScoped<IScheduledJobExecutionRepository, ScheduledJobExecutionRepository>();

        services.AddSingleton(new SchedulingServiceOptions
        {
            DefaultTimeZone = schedulingOptions.DefaultTimeZone,
            MinimumCronIntervalHours = schedulingOptions.MinimumCronIntervalHours
        });
        services.AddScoped<ISchedulingService, SchedulingService>();

        services.AddSingleton<ScheduledJobListener>();

        services.AddQuartz(q =>
        {
            q.SchedulerId = "AUTO";
            q.SchedulerName = "DonkeyWorkScheduler";

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

            q.AddJobListener<ScheduledJobListener>(GroupMatcher<JobKey>.AnyGroup());
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        services.AddHostedService<ScheduleReconciliationService>();

        return services;
    }
}
