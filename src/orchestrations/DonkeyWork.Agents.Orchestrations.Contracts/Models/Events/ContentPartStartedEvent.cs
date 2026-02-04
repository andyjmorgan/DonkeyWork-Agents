namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;

/// <summary>
/// Signals the start of a content part block in the model response.
/// </summary>
public class ContentPartStartedEvent : ExecutionEvent
{
    /// <summary>
    /// The index of the content block (0-based).
    /// </summary>
    public int BlockIndex { get; set; }

    /// <summary>
    /// The type of content part.
    /// </summary>
    public ContentPartType ContentType { get; set; }
}
