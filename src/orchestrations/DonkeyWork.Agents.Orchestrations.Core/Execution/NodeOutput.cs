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

    /// <summary>
    /// Returns a projection safe to persist into trace events and per-step
    /// Input/Output records. Default returns the output itself; outputs that
    /// hold large binary blobs (e.g. base64 audio) override this to swap the
    /// blob for a placeholder so trace event payloads stay below NATS' 1 MB
    /// max message size. Downstream nodes still receive the real, unredacted
    /// output from the in-memory execution context.
    /// </summary>
    public virtual object ToTraceOutput() => this;
}
