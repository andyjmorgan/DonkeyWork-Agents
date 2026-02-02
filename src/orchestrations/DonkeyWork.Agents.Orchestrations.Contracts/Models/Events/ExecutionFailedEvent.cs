namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;

public class ExecutionFailedEvent : ExecutionEvent
{
    public string ErrorMessage { get; set; } = string.Empty;
}
