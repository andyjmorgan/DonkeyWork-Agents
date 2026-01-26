namespace DonkeyWork.Agents.Agents.Contracts.Models.Events;

public class NodeCompletedEvent : ExecutionEvent
{
    public string NodeId { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
}
