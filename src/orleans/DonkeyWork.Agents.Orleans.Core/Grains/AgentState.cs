using DonkeyWork.Agents.Orleans.Contracts.Messages;

namespace DonkeyWork.Agents.Orleans.Core.Grains;

[GenerateSerializer]
public sealed class AgentState
{
    [Id(0)] public List<InternalMessage> Messages { get; set; } = [];
    [Id(1)] public string ConversationId { get; set; } = string.Empty;
}
