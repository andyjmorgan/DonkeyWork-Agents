namespace DonkeyWork.Agents.Agents.Contracts.Models.Events;

public class NodeStartedEvent : ExecutionEvent
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
}
