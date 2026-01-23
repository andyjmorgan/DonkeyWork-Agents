namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;

/// <summary>
/// Tool call being executed.
/// </summary>
public class ToolCallEvent : ModelPipelineEvent
{
    /// <summary>
    /// Unique identifier for this tool call.
    /// </summary>
    public required string CallId { get; set; }

    /// <summary>
    /// Name of the tool being called.
    /// </summary>
    public required string ToolName { get; set; }

    /// <summary>
    /// JSON-encoded arguments.
    /// </summary>
    public required string Arguments { get; set; }
}
