namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;

/// <summary>
/// Request to execute the model pipeline.
/// </summary>
public class ModelPipelineRequest
{
    /// <summary>
    /// Conversation history.
    /// </summary>
    public required List<ChatMessage> Messages { get; set; }

    /// <summary>
    /// Model configuration.
    /// </summary>
    public required PipelineModelConfig Model { get; set; }

    /// <summary>
    /// Available tools for the model to call.
    /// </summary>
    public List<PipelineToolDefinition>? Tools { get; set; }

    /// <summary>
    /// Provider-specific options (temperature, max_tokens, thinking_budget, etc.).
    /// </summary>
    public Dictionary<string, object>? Options { get; set; }
}
