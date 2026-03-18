using DonkeyWork.Agents.Actors.Contracts.Messages;

namespace DonkeyWork.Agents.Actors.Contracts.Models;

/// <summary>
/// Response containing the message history for an agent execution.
/// </summary>
public sealed class GetAgentExecutionMessagesResponseV1
{
    public required IReadOnlyList<InternalMessage> Messages { get; init; }
}
