namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;

public class TokenDeltaEvent : ExecutionEvent
{
    public string Delta { get; set; } = string.Empty;
}
