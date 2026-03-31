namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

public sealed class ToolEnabledOrchestrationV1
{
    public required GetOrchestrationResponseV1 Orchestration { get; init; }
    public required GetOrchestrationVersionResponseV1 Version { get; init; }
}
