namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;

/// <summary>
/// Result of a tool execution.
/// </summary>
public class ToolResultEvent : ModelPipelineEvent
{
    /// <summary>
    /// Unique identifier for this tool call.
    /// </summary>
    public required string CallId { get; set; }

    /// <summary>
    /// Name of the tool that was called.
    /// </summary>
    public required string ToolName { get; set; }

    /// <summary>
    /// The tool's response.
    /// </summary>
    public required string Result { get; set; }

    /// <summary>
    /// Whether the tool execution succeeded.
    /// </summary>
    public bool Success { get; set; } = true;
}
