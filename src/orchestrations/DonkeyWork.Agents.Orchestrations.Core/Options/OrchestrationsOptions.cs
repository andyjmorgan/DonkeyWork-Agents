using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Orchestrations.Core.Options;

public class OrchestrationsOptions
{
    public const string SectionName = "Agents";

    [Required]
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(20);

    [Required]
    public TimeSpan StreamRetention { get; set; } = TimeSpan.FromHours(24);

    public long StreamMaxBytes { get; set; } = 1_073_741_824;
}
