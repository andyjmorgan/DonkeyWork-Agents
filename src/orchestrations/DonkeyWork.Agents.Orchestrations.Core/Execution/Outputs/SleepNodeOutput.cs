namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// Output from a Sleep node execution.
/// </summary>
public sealed class SleepNodeOutput : NodeOutput
{
    /// <summary>
    /// The actual duration slept in seconds.
    /// </summary>
    public required double DurationSeconds { get; init; }

    public override string ToMessageOutput() => $"Slept for {DurationSeconds}s";
}
