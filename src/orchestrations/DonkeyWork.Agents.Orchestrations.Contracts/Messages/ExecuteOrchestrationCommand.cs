using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Messages;

public record ExecuteOrchestrationCommand
{
    public required Guid ExecutionId { get; init; }

    public required Guid UserId { get; init; }

    public string? UserEmail { get; init; }

    public string? UserName { get; init; }

    public string? UserUsername { get; init; }

    public required Guid VersionId { get; init; }

    public required ExecutionInterface ExecutionInterface { get; init; }

    public string? InputJson { get; init; }

    public string? ConversationJson { get; init; }
}
