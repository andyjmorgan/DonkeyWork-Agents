using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Scheduling.Api.Options;

public class SchedulingOptions
{
    public const string SectionName = "Scheduling";

    [Required]
    public string DefaultTimeZone { get; set; } = "Europe/Dublin";

    [Range(1, 365)]
    public int ExecutionHistoryRetentionDays { get; set; } = 30;

    [Range(1, 24)]
    public int MinimumCronIntervalHours { get; set; } = 4;

    [Range(1, 365)]
    public int CompletedOneOffRetentionDays { get; set; } = 7;
}
