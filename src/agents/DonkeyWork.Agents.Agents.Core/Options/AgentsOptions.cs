using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Agents.Core.Options;

public class AgentsOptions
{
    public const string SectionName = "Agents";

    [Required]
    public TimeSpan ExecutionTimeout { get; set; } = TimeSpan.FromMinutes(20);

    [Required]
    public TimeSpan StreamRetention { get; set; } = TimeSpan.FromHours(24);
}
