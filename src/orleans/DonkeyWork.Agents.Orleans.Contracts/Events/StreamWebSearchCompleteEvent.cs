namespace DonkeyWork.Agents.Orleans.Contracts.Events;

[GenerateSerializer]
public sealed record StreamWebSearchCompleteEvent(
    string AgentKey,
    [property: Id(0)] string ToolUseId,
    [property: Id(1)] List<WebSearchResultEntry> Results) : StreamEventBase(AgentKey)
{
    public override string EventType => "web_search_complete";
}

[GenerateSerializer]
public sealed record WebSearchResultEntry(
    [property: Id(0)] string Title,
    [property: Id(1)] string Url);
