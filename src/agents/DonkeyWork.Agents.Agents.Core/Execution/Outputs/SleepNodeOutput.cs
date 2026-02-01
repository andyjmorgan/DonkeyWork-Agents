namespace DonkeyWork.Agents.Agents.Core.Execution.Outputs;

/// <summary>
/// Output from a Sleep node execution.
/// </summary>
public sealed class SleepNodeOutput : NodeOutput
{
    /// <summary>
    /// The actual duration slept in milliseconds.
    /// </summary>
    public required int DurationMs { get; init; }

    public override string ToMessageOutput() => $"Slept for {DurationMs}ms";
}
