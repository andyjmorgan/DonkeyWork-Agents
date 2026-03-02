namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamMcpServerStatusEvent(
    string AgentKey,
    [property: Id(0)] string ServerName,
    [property: Id(1)] bool Success,
    [property: Id(2)] long DurationMs,
    [property: Id(3)] int ToolCount,
    [property: Id(4)] string? Error) : StreamEventBase(AgentKey)
{
    public override string EventType => "mcp_server_status";
}
