namespace DonkeyWork.Agents.Agents.Core.Execution.Outputs;

/// <summary>
/// Output from an End node execution.
/// </summary>
public class EndNodeOutput : NodeOutput
{
    /// <summary>
    /// The final output from the execution.
    /// </summary>
    public required object FinalOutput { get; init; }

    /// <summary>
    /// Converts the output to a string for message output.
    /// </summary>
    public override string ToMessageOutput()
    {
        if (FinalOutput is string str)
        {
            return str;
        }

        return System.Text.Json.JsonSerializer.Serialize(FinalOutput);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (FinalOutput is string str)
        {
            return str;
        }

        return System.Text.Json.JsonSerializer.Serialize(FinalOutput);
    }
}
