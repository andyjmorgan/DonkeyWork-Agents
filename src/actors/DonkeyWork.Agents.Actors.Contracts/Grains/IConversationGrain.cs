using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Contracts.Models;

namespace DonkeyWork.Agents.Actors.Contracts.Grains;

public interface IConversationGrain : IGrainWithStringKey
{
    Task SubscribeAsync(IAgentResponseObserver observer);

    Task PostUserMessageAsync(string message);

    Task DeliverAgentResultAsync(string agentKey, string label, AgentResult? result, bool isError);

    Task CancelByKeyAsync(string key, string? scope = null);

    Task<IReadOnlyList<TrackedAgent>> ListAgentsAsync();

    Task<IReadOnlyList<InternalMessage>> GetMessagesAsync();

    Task<IReadOnlyList<InternalMessage>> GetAgentMessagesAsync(string agentKey);
}
