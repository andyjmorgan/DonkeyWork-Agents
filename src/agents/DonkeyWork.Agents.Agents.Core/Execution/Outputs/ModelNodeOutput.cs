namespace DonkeyWork.Agents.Agents.Core.Execution.Outputs;

/// <summary>
/// Output from a Model node execution.
/// </summary>
public class ModelNodeOutput : NodeOutput
{
    /// <summary>
    /// The complete LLM response text.
    /// </summary>
    public required string ResponseText { get; init; }

    /// <summary>
    /// Total tokens used (input + output).
    /// </summary>
    public int? TotalTokens { get; init; }

    /// <summary>
    /// Input tokens used.
    /// </summary>
    public int? InputTokens { get; init; }

    /// <summary>
    /// Output tokens used.
    /// </summary>
    public int? OutputTokens { get; init; }

    /// <summary>
    /// Converts the output to a string for message output.
    /// </summary>
    public override string ToMessageOutput()
    {
        return ResponseText;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return ResponseText;
    }
}
