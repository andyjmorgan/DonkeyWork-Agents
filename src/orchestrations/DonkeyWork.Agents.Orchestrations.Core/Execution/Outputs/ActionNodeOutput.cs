namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// Output from an Action node execution.
/// </summary>
public sealed class ActionNodeOutput : NodeOutput
{
    /// <summary>
    /// The result returned by the action provider.
    /// </summary>
    public required object Result { get; init; }

    /// <summary>
    /// The action type that was executed.
    /// </summary>
    public required string ActionType { get; init; }

    public override string ToMessageOutput()
    {
        // Convert the result to a JSON string for downstream nodes
        return System.Text.Json.JsonSerializer.Serialize(Result);
    }
}
