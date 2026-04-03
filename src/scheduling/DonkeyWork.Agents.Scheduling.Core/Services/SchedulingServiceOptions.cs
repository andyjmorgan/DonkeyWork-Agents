namespace DonkeyWork.Agents.Scheduling.Core.Services;

public class SchedulingServiceOptions
{
    public string DefaultTimeZone { get; set; } = "Europe/Dublin";

    public int MinimumCronIntervalHours { get; set; } = 4;

    public int ExecutionHistoryRetentionDays { get; set; } = 30;

    public int CompletedOneOffRetentionDays { get; set; } = 7;
}
