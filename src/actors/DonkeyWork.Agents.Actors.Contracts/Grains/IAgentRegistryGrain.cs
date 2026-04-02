using DonkeyWork.Agents.Actors.Contracts.Models;

namespace DonkeyWork.Agents.Actors.Contracts.Grains;

public interface IAgentRegistryGrain : IGrainWithStringKey
{
    Task<string> RegisterAsync(string agentKey, string label, string name, string parentAgentKey, TimeSpan? timeout = null);

    Task ReportCompletionAsync(string agentKey, AgentResult result, bool isError = false);

    Task<AgentWaitResult?> WaitForNextAsync(TimeSpan? timeout = null);

    Task<AgentWaitResult?> WaitForSpecificAsync(string agentKey, TimeSpan? timeout = null);

    Task<IReadOnlyList<TrackedAgent>> ListAsync();

    Task<string?> ResolveAgentKeyByNameAsync(string name);

    Task<bool> SendMessageAsync(string fromAgentKey, string toAgentKey, AgentMessage message);

    Task BroadcastMessageAsync(string fromAgentKey, AgentMessage message);

    Task ReportIdleAsync(string agentKey);

    Task WriteSharedContextAsync(string key, string value);

    Task<string?> ReadSharedContextAsync(string key);

    Task<IReadOnlyDictionary<string, string>> ReadAllSharedContextAsync();

    Task<bool> RemoveSharedContextAsync(string key);
}
