namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;

/// <summary>
/// Signals the end of a content part block in the model response.
/// </summary>
public class ContentPartEndEvent : ModelPipelineEvent
{
    /// <summary>
    /// The index of the content block that ended (0-based).
    /// </summary>
    public required int BlockIndex { get; set; }
}
