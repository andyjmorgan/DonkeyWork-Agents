namespace DonkeyWork.Agents.Agents.Contracts.Models.Events;

public class TokenDeltaEvent : ExecutionEvent
{
    public string Delta { get; set; } = string.Empty;
}
