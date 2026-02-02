namespace DonkeyWork.Agents.Orchestrations.Core.Execution;

/// <summary>
/// Base class for all node outputs.
/// </summary>
public abstract class NodeOutput
{
    /// <summary>
    /// Converts the output to a string suitable for LLM message content.
    /// Override in derived classes to provide custom formatting.
    /// </summary>
    /// <returns>String representation for message output.</returns>
    public virtual string ToMessageOutput() => ToString() ?? string.Empty;
}
