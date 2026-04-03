namespace DonkeyWork.Agents.Scheduling.Core.Services;

public class SchedulingServiceOptions
{
    public string DefaultTimeZone { get; set; } = "Europe/Dublin";

    public int MinimumCronIntervalHours { get; set; } = 4;
}
