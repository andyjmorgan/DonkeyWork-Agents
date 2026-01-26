namespace DonkeyWork.Agents.Agents.Contracts.Models.Events;

public class ExecutionCompletedEvent : ExecutionEvent
{
    public string Output { get; set; } = string.Empty;
}
