namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Outputs;

/// <summary>
/// Output from a Start node execution.
/// </summary>
public class StartNodeOutput : NodeOutput
{
    /// <summary>
    /// The validated input data.
    /// </summary>
    public required object Input { get; init; }

    /// <summary>
    /// Converts the output to a JSON string for message output.
    /// </summary>
    public override string ToMessageOutput()
    {
        return System.Text.Json.JsonSerializer.Serialize(Input);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(Input);
    }
}
