namespace DonkeyWork.Agents.Orleans.Contracts.Events;

[GenerateSerializer]
public sealed record StreamAgentResultDataEvent(
    string AgentKey,
    [property: Id(0)] string SubAgentKey,
    [property: Id(1)] string AgentType,
    [property: Id(2)] string Label,
    [property: Id(3)] string? Text,
    [property: Id(4)] List<StreamAgentCitation>? Citations,
    [property: Id(5)] bool IsError) : StreamEventBase(AgentKey)
{
    public override string EventType => "agent_result_data";
}

[GenerateSerializer]
public sealed record StreamAgentCitation(
    [property: Id(0)] string Title,
    [property: Id(1)] string Url,
    [property: Id(2)] string CitedText);
