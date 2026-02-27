namespace DonkeyWork.Agents.Actors.Contracts.Models;

[GenerateSerializer]
public enum AgentStatus
{
    Pending,
    Completed,
    Failed,
    TimedOut,
    Cancelled
}
