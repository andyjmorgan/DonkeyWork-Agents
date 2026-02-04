namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;

/// <summary>
/// Signals the end of a content part block in the model response.
/// </summary>
public class ContentPartEndedEvent : ExecutionEvent
{
    /// <summary>
    /// The index of the content block that ended (0-based).
    /// </summary>
    public int BlockIndex { get; set; }
}
