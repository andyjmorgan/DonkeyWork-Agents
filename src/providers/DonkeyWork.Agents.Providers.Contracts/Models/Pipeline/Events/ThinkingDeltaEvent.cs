namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;

/// <summary>
/// Thinking/reasoning content chunk from the model.
/// </summary>
public class ThinkingDeltaEvent : ModelPipelineEvent
{
    /// <summary>
    /// The thinking content chunk.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// Whether this is encrypted reasoning (opaque, not human-readable).
    /// </summary>
    public bool IsEncrypted { get; set; }
}
