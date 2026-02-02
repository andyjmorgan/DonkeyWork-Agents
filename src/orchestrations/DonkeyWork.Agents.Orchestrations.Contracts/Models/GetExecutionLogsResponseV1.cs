namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Response containing execution logs.
/// </summary>
public class GetExecutionLogsResponseV1
{
    /// <summary>
    /// List of log entries.
    /// </summary>
    public IReadOnlyList<ExecutionLogV1> Logs { get; set; } = Array.Empty<ExecutionLogV1>();

    /// <summary>
    /// Total number of log entries available (for pagination).
    /// </summary>
    public int TotalCount { get; set; }
}
