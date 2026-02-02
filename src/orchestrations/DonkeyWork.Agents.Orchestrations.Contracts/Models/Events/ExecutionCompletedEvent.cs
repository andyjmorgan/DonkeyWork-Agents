namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;

public class ExecutionCompletedEvent : ExecutionEvent
{
    public string Output { get; set; } = string.Empty;
}
