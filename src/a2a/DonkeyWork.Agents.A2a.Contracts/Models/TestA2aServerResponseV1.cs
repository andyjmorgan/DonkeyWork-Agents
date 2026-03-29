namespace DonkeyWork.Agents.A2a.Contracts.Models;

public sealed class TestA2aServerResponseV1
{
    public required bool Success { get; init; }

    public long ElapsedMs { get; init; }

    public string? Error { get; init; }

    public A2aAgentCardV1? AgentCard { get; init; }
}
