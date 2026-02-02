namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;

public class NodeStartedEvent : ExecutionEvent
{
    public Guid NodeId { get; set; }
    public string NodeType { get; set; } = string.Empty;
}
