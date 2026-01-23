namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline.Events;

/// <summary>
/// Text content chunk from the model.
/// </summary>
public class TextDeltaEvent : ModelPipelineEvent
{
    /// <summary>
    /// The text chunk.
    /// </summary>
    public required string Text { get; set; }
}
