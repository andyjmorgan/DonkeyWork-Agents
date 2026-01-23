namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;

/// <summary>
/// Definition of a tool available to the model.
/// </summary>
public class PipelineToolDefinition
{
    /// <summary>
    /// Unique name of the tool.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of what the tool does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// JSON schema for the tool's input parameters.
    /// </summary>
    public string? InputSchema { get; set; }
}
