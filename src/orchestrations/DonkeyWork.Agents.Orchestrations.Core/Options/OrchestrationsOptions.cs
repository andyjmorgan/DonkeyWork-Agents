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

    /// <summary>
    /// Maximum number of orchestration executions handled concurrently by the Wolverine listener.
    /// Replaces the old global <c>.Sequential()</c> behaviour so long-running executions
    /// (e.g. chunked TTS) don't block other users' queues. Tune against provider rate limits.
    /// </summary>
    [Range(1, 256)]
    public int MaxConcurrentExecutions { get; set; } = 8;
}
