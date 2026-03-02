using DonkeyWork.Agents.Actors.Contracts.Events;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Protocol;

public class StreamEventTests
{
    private const string TestAgentKey = "conv:user1:conv1";

    #region EventType Tests

    [Fact]
    public void StreamMessageEvent_EventType_ReturnsMessage()
    {
        var evt = new StreamMessageEvent(TestAgentKey, "hello");
        Assert.Equal("message", evt.EventType);
    }

    [Fact]
    public void StreamThinkingEvent_EventType_ReturnsThinking()
    {
        var evt = new StreamThinkingEvent(TestAgentKey, "thinking...");
        Assert.Equal("thinking", evt.EventType);
    }

    [Fact]
    public void StreamToolUseEvent_EventType_ReturnsToolUse()
    {
        var evt = new StreamToolUseEvent(TestAgentKey, "search", "tool-1", "{}");
        Assert.Equal("tool_use", evt.EventType);
    }

    [Fact]
    public void StreamToolCompleteEvent_EventType_ReturnsToolComplete()
    {
        var evt = new StreamToolCompleteEvent(TestAgentKey, "tool-1", "search", true, 100);
        Assert.Equal("tool_complete", evt.EventType);
    }

    [Fact]
    public void StreamProgressEvent_EventType_ReturnsProgress()
    {
        var evt = new StreamProgressEvent(TestAgentKey, "Searching...");
        Assert.Equal("progress", evt.EventType);
    }

    [Fact]
    public void StreamAgentSpawnEvent_EventType_ReturnsAgentSpawn()
    {
        var evt = new StreamAgentSpawnEvent(TestAgentKey, "research:1:2:3", "Research");
        Assert.Equal("agent_spawn", evt.EventType);
    }

    [Fact]
    public void StreamAgentCompleteEvent_EventType_ReturnsAgentComplete()
    {
        var evt = new StreamAgentCompleteEvent(TestAgentKey);
        Assert.Equal("agent_complete", evt.EventType);
    }

    [Fact]
    public void StreamCompleteEvent_EventType_ReturnsComplete()
    {
        var evt = new StreamCompleteEvent(TestAgentKey, "done");
        Assert.Equal("complete", evt.EventType);
    }

    [Fact]
    public void StreamErrorEvent_EventType_ReturnsError()
    {
        var evt = new StreamErrorEvent(TestAgentKey, "something failed");
        Assert.Equal("error", evt.EventType);
    }

    [Fact]
    public void StreamCancelledEvent_EventType_ReturnsCancelled()
    {
        var evt = new StreamCancelledEvent(TestAgentKey, "Active");
        Assert.Equal("cancelled", evt.EventType);
    }

    [Fact]
    public void StreamMcpServerStatusEvent_EventType_ReturnsMcpServerStatus()
    {
        var evt = new StreamMcpServerStatusEvent(TestAgentKey, "my-server", true, 142, 8, null);
        Assert.Equal("mcp_server_status", evt.EventType);
    }

    [Fact]
    public void StreamMcpServerStatusEvent_PreservesAllProperties()
    {
        var evt = new StreamMcpServerStatusEvent(TestAgentKey, "test-server", false, 5000, 0, "Connection timed out");

        Assert.Equal("test-server", evt.ServerName);
        Assert.False(evt.Success);
        Assert.Equal(5000, evt.DurationMs);
        Assert.Equal(0, evt.ToolCount);
        Assert.Equal("Connection timed out", evt.Error);
    }

    #endregion

    #region AgentKey Propagation Tests

    [Fact]
    public void AllStreamEvents_PreserveAgentKey()
    {
        StreamEventBase[] events =
        [
            new StreamMessageEvent(TestAgentKey, "text"),
            new StreamThinkingEvent(TestAgentKey, "thought"),
            new StreamToolUseEvent(TestAgentKey, "tool", "id", "{}"),
            new StreamToolResultEvent(TestAgentKey, "id", "tool", "result", true, 50),
            new StreamToolCompleteEvent(TestAgentKey, "id", "tool", true, 50),
            new StreamWebSearchEvent(TestAgentKey, "id", "query"),
            new StreamCitationEvent(TestAgentKey, "title", "url", "text"),
            new StreamUsageEvent(TestAgentKey, 100, 200, 1),
            new StreamProgressEvent(TestAgentKey, "breadcrumb"),
            new StreamAgentSpawnEvent(TestAgentKey, "child-key", "Research"),
            new StreamAgentCompleteEvent(TestAgentKey),
            new StreamCompleteEvent(TestAgentKey, "done"),
            new StreamErrorEvent(TestAgentKey, "error"),
            new StreamTurnStartEvent(TestAgentKey, "user", "preview"),
            new StreamTurnEndEvent(TestAgentKey),
            new StreamQueueStatusEvent(TestAgentKey, 0, false),
            new StreamCancelledEvent(TestAgentKey, "Both"),
            new StreamMcpServerStatusEvent(TestAgentKey, "server", true, 100, 5, null),
        ];

        foreach (var evt in events)
        {
            Assert.Equal(TestAgentKey, evt.AgentKey);
        }
    }

    #endregion
}
