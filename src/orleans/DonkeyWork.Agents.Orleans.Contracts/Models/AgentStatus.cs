namespace DonkeyWork.Agents.Orleans.Contracts.Models;

[GenerateSerializer]
public enum AgentStatus
{
    Pending,
    Completed,
    Failed,
    TimedOut,
    Cancelled
}
