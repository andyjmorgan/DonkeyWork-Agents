namespace DonkeyWork.Agents.Agents.Core.Execution;

/// <summary>
/// Context that maintains state during agent execution.
/// </summary>
public class ExecutionContext
{
    /// <summary>
    /// Unique identifier for this execution.
    /// </summary>
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Dictionary of node outputs keyed by node name.
    /// Used for template variable resolution (accessible as 'steps' in Scriban).
    /// </summary>
    public Dictionary<string, object> NodeOutputs { get; } = new();

    /// <summary>
    /// The input provided to the execution.
    /// Validated against the agent version's InputSchema.
    /// </summary>
    public required object Input { get; init; }

    /// <summary>
    /// The user ID that owns this execution.
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    /// The JSON Schema for input validation.
    /// </summary>
    public required string InputSchema { get; init; }
}
