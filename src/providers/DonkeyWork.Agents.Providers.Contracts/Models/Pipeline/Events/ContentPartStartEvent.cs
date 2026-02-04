namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;

/// <summary>
/// Signals the start of a content part block in the model response.
/// </summary>
public class ContentPartStartEvent : ModelPipelineEvent
{
    /// <summary>
    /// The index of the content block (0-based).
    /// </summary>
    public required int BlockIndex { get; set; }

    /// <summary>
    /// The type of content part.
    /// </summary>
    public required ContentPartType Type { get; set; }
}
