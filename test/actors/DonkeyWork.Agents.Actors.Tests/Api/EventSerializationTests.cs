using System.Text.Json;
using DonkeyWork.Agents.Actors.Api.Observers;
using DonkeyWork.Agents.Actors.Contracts.Events;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Api;

public class EventSerializationTests
{
    #region StreamMessageEvent Tests

    [Fact]
    public void Serialize_StreamMessageEvent_ProducesCamelCaseJson()
    {
        var evt = new StreamMessageEvent("agent-1", "Hello world");
        var json = Serialize(evt, AgentJsonContext.Default.StreamMessageEvent);

        Assert.Contains("\"agentKey\"", json);
        Assert.Contains("\"eventType\"", json);
        Assert.Contains("\"text\"", json);
        Assert.Contains("\"message\"", json);
        Assert.Contains("Hello world", json);
    }

    #endregion

    #region StreamThinkingEvent Tests

    [Fact]
    public void Serialize_StreamThinkingEvent_ContainsThinkingText()
    {
        var evt = new StreamThinkingEvent("agent-1", "reasoning here");
        var json = Serialize(evt, AgentJsonContext.Default.StreamThinkingEvent);

        Assert.Contains("\"thinking\"", json);
        Assert.Contains("reasoning here", json);
    }

    #endregion

    #region StreamToolUseEvent Tests

    [Fact]
    public void Serialize_StreamToolUseEvent_ContainsToolFields()
    {
        var evt = new StreamToolUseEvent("agent-1", "search", "tool-1", "{\"q\":\"test\"}")
        {
            DisplayName = "Web Search",
        };
        var json = Serialize(evt, AgentJsonContext.Default.StreamToolUseEvent);

        Assert.Contains("\"toolName\"", json);
        Assert.Contains("\"toolUseId\"", json);
        Assert.Contains("\"arguments\"", json);
        Assert.Contains("\"displayName\"", json);
        Assert.Contains("Web Search", json);
    }

    #endregion

    #region StreamWebSearchCompleteEvent Tests

    [Fact]
    public void Serialize_StreamWebSearchCompleteEvent_ContainsResults()
    {
        var evt = new StreamWebSearchCompleteEvent("agent-1", "tool-1",
        [
            new WebSearchResultEntry("Example", "https://example.com"),
        ]);
        var json = Serialize(evt, AgentJsonContext.Default.StreamWebSearchCompleteEvent);

        Assert.Contains("\"results\"", json);
        Assert.Contains("\"title\"", json);
        Assert.Contains("\"url\"", json);
        Assert.Contains("Example", json);
    }

    #endregion

    #region StreamAgentResultDataEvent Tests

    [Fact]
    public void Serialize_StreamAgentResultDataEvent_ContainsCitations()
    {
        var evt = new StreamAgentResultDataEvent(
            "agent-1", "sub-1", "research", "Researcher",
            "Some results",
            [new StreamAgentCitation("Title", "https://example.com", "cited text")],
            false);
        var json = Serialize(evt, AgentJsonContext.Default.StreamAgentResultDataEvent);

        Assert.Contains("\"subAgentKey\"", json);
        Assert.Contains("\"agentType\"", json);
        Assert.Contains("\"citations\"", json);
        Assert.Contains("\"citedText\"", json);
    }

    #endregion

    #region StreamAgentCompleteEvent Tests

    [Fact]
    public void Serialize_StreamAgentCompleteEvent_ReasonIsString()
    {
        var evt = new StreamAgentCompleteEvent("agent-1") { Reason = AgentCompleteReason.Completed };
        var json = Serialize(evt, AgentJsonContext.Default.StreamAgentCompleteEvent);

        Assert.Contains("\"reason\"", json);
        Assert.Contains("\"Completed\"", json); // UseStringEnumConverter
    }

    #endregion

    #region StreamErrorEvent Tests

    [Fact]
    public void Serialize_StreamErrorEvent_ContainsError()
    {
        var evt = new StreamErrorEvent("agent-1", "Something broke");
        var json = Serialize(evt, AgentJsonContext.Default.StreamErrorEvent);

        Assert.Contains("\"error\"", json);
        Assert.Contains("Something broke", json);
    }

    #endregion

    #region StreamQueueStatusEvent Tests

    [Fact]
    public void Serialize_StreamQueueStatusEvent_ContainsStatusFields()
    {
        var evt = new StreamQueueStatusEvent("agent-1", 3, true);
        var json = Serialize(evt, AgentJsonContext.Default.StreamQueueStatusEvent);

        Assert.Contains("\"pendingCount\"", json);
        Assert.Contains("\"isProcessing\"", json);
    }

    #endregion

    #region StreamMcpServerStatusEvent Tests

    [Fact]
    public void Serialize_StreamMcpServerStatusEvent_Success_ContainsAllFields()
    {
        var evt = new StreamMcpServerStatusEvent("agent-1", "my-mcp-server", true, 142, 8, null);
        var json = Serialize(evt, AgentJsonContext.Default.StreamMcpServerStatusEvent);

        Assert.Contains("\"eventType\"", json);
        Assert.Contains("\"mcp_server_status\"", json);
        Assert.Contains("\"serverName\"", json);
        Assert.Contains("my-mcp-server", json);
        Assert.Contains("\"success\"", json);
        Assert.Contains("\"durationMs\"", json);
        Assert.Contains("\"toolCount\"", json);
        Assert.Contains("142", json);
        Assert.Contains("8", json);
    }

    [Fact]
    public void Serialize_StreamMcpServerStatusEvent_Failure_ContainsError()
    {
        var evt = new StreamMcpServerStatusEvent("agent-1", "broken-server", false, 30000, 0, "Connection timed out");
        var json = Serialize(evt, AgentJsonContext.Default.StreamMcpServerStatusEvent);

        Assert.Contains("\"error\"", json);
        Assert.Contains("Connection timed out", json);
        Assert.Contains("\"success\":false", json.Replace(" ", ""));
    }

    #endregion

    #region All Event Types Tests

    [Fact]
    public void AllEventTypes_SerializeToValidJson()
    {
        var events = CreateAllEventTypes();

        foreach (var (evt, typeInfo) in events)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(evt, typeInfo);
            using var doc = JsonDocument.Parse(bytes);

            // Every event should have agentKey and eventType
            Assert.True(doc.RootElement.TryGetProperty("agentKey", out _),
                $"Missing agentKey in {evt.GetType().Name}");
            Assert.True(doc.RootElement.TryGetProperty("eventType", out _),
                $"Missing eventType in {evt.GetType().Name}");
        }
    }

    #endregion

    #region Helpers

    private static string Serialize<T>(T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        return JsonSerializer.Serialize(value, typeInfo);
    }

    private static List<(StreamEventBase Event, System.Text.Json.Serialization.Metadata.JsonTypeInfo TypeInfo)> CreateAllEventTypes()
    {
        return
        [
            (new StreamMessageEvent("a", "text"), AgentJsonContext.Default.StreamMessageEvent),
            (new StreamThinkingEvent("a", "think"), AgentJsonContext.Default.StreamThinkingEvent),
            (new StreamToolUseEvent("a", "tool", "id", "{}"), AgentJsonContext.Default.StreamToolUseEvent),
            (new StreamToolResultEvent("a", "id", "tool", "result", true, 100), AgentJsonContext.Default.StreamToolResultEvent),
            (new StreamToolCompleteEvent("a", "id", "tool", true, 100), AgentJsonContext.Default.StreamToolCompleteEvent),
            (new StreamWebSearchEvent("a", "id", "query"), AgentJsonContext.Default.StreamWebSearchEvent),
            (new StreamWebSearchCompleteEvent("a", "id", []), AgentJsonContext.Default.StreamWebSearchCompleteEvent),
            (new StreamCitationEvent("a", "title", "url", "text"), AgentJsonContext.Default.StreamCitationEvent),
            (new StreamUsageEvent("a", 10, 20, 0, 200000, 8192), AgentJsonContext.Default.StreamUsageEvent),
            (new StreamProgressEvent("a", "doing stuff"), AgentJsonContext.Default.StreamProgressEvent),
            (new StreamAgentSpawnEvent("a", "sub-1", "research"), AgentJsonContext.Default.StreamAgentSpawnEvent),
            (new StreamAgentCompleteEvent("a"), AgentJsonContext.Default.StreamAgentCompleteEvent),
            (new StreamAgentResultDataEvent("a", "sub-1", "research", "label", "text", null, false), AgentJsonContext.Default.StreamAgentResultDataEvent),
            (new StreamCompleteEvent("a", "done"), AgentJsonContext.Default.StreamCompleteEvent),
            (new StreamErrorEvent("a", "err"), AgentJsonContext.Default.StreamErrorEvent),
            (new StreamTurnStartEvent("a", "user", "Hello"), AgentJsonContext.Default.StreamTurnStartEvent),
            (new StreamTurnEndEvent("a"), AgentJsonContext.Default.StreamTurnEndEvent),
            (new StreamQueueStatusEvent("a", 0, false), AgentJsonContext.Default.StreamQueueStatusEvent),
            (new StreamCancelledEvent("a", "active"), AgentJsonContext.Default.StreamCancelledEvent),
            (new StreamMcpServerStatusEvent("a", "server", true, 100, 5, null), AgentJsonContext.Default.StreamMcpServerStatusEvent),
        ];
    }

    #endregion
}
