namespace CodeSandbox.Contracts.Responses;

/// <summary>
/// Information about a tracked process that may still be running.
/// </summary>
public class TrackedProcessInfo
{
    public int Pid { get; set; }
    public string Command { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? ExitCode { get; set; }
    public int BufferedEventCount { get; set; }
}
