namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;

public class NodeCompletedEvent : ExecutionEvent
{
    public Guid NodeId { get; set; }
    public string Output { get; set; } = string.Empty;
}
