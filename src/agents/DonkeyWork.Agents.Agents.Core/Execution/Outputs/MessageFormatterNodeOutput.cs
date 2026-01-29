namespace DonkeyWork.Agents.Agents.Core.Execution.Outputs;

/// <summary>
/// Output from a Message Formatter node execution.
/// </summary>
public class MessageFormatterNodeOutput : NodeOutput
{
    /// <summary>
    /// The rendered template result.
    /// </summary>
    public required string FormattedMessage { get; init; }

    /// <summary>
    /// Converts the output to a string for message output.
    /// </summary>
    public override string ToMessageOutput()
    {
        return FormattedMessage;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return FormattedMessage;
    }
}
