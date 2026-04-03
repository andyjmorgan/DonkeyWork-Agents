using Quartz;

namespace DonkeyWork.Agents.Scheduling.Core.Quartz;

public static class QuartzKeyFactory
{
    private const string JobGroup = "scheduled-jobs";
    private const string TriggerGroup = "scheduled-triggers";
    private const string SystemJobGroup = "system-jobs";
    private const string SystemTriggerGroup = "system-triggers";

    public static JobKey CreateJobKey(Guid scheduleId, bool isSystem = false)
        => new($"schedule-{scheduleId}", isSystem ? SystemJobGroup : JobGroup);

    public static TriggerKey CreateTriggerKey(Guid scheduleId, bool isSystem = false)
        => new($"trigger-{scheduleId}", isSystem ? SystemTriggerGroup : TriggerGroup);

    public static string FormatJobKey(JobKey key) => $"{key.Group}.{key.Name}";

    public static string FormatTriggerKey(TriggerKey key) => $"{key.Group}.{key.Name}";
}
