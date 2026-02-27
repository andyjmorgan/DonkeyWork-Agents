using DonkeyWork.Agents.Actors.Contracts.Models;

namespace DonkeyWork.Agents.Actors.Contracts.Grains;

public interface IAgentRegistryGrain : IGrainWithStringKey
{
    Task RegisterAsync(string agentKey, string label, string parentAgentKey, TimeSpan? timeout = null);

    Task ReportCompletionAsync(string agentKey, AgentResult result, bool isError = false);

    Task<AgentWaitResult?> WaitForNextAsync(TimeSpan? timeout = null);

    Task<AgentWaitResult?> WaitForSpecificAsync(string agentKey, TimeSpan? timeout = null);

    Task<IReadOnlyList<TrackedAgent>> ListAsync();
}
