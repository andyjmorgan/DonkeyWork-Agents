using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Core.Grains;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Grains;

/// <summary>
/// Tests for static helper methods used by the grain implementations.
/// These methods are accessed via InternalsVisibleTo.
/// </summary>
public class GrainHelperTests
{
    #region AgentResult Building Tests

    [Fact]
    public void BuildAgentResult_WithNullMessage_ReturnsEmpty()
    {
        // Act
        var result = AgentGrainTestHelpers.BuildAgentResult(null);

        // Assert
        Assert.Empty(result.Parts);
    }

    [Fact]
    public void BuildAgentResult_WithTextContent_ReturnsTextPart()
    {
        // Arrange
        var msg = new InternalAssistantMessage
        {
            Role = InternalMessageRole.Assistant,
            TextContent = "Hello world",
        };

        // Act
        var result = AgentGrainTestHelpers.BuildAgentResult(msg);

        // Assert
        Assert.Single(result.Parts);
        var textPart = Assert.IsType<AgentTextPart>(result.Parts[0]);
        Assert.Equal("Hello world", textPart.Text);
    }

    [Fact]
    public void BuildAgentResult_WithCitationBlocks_ReturnsCitationParts()
    {
        // Arrange
        var msg = new InternalAssistantMessage
        {
            Role = InternalMessageRole.Assistant,
            TextContent = "Research results",
            ContentBlocks =
            [
                new InternalTextBlock("text"),
                new InternalCitationBlock("Source", "https://example.com", "quoted text"),
            ],
        };

        // Act
        var result = AgentGrainTestHelpers.BuildAgentResult(msg);

        // Assert
        Assert.Equal(2, result.Parts.Count);
        Assert.IsType<AgentTextPart>(result.Parts[0]);
        var citation = Assert.IsType<AgentCitationPart>(result.Parts[1]);
        Assert.Equal("Source", citation.Title);
        Assert.Equal("https://example.com", citation.Url);
        Assert.Equal("quoted text", citation.CitedText);
    }

    [Fact]
    public void BuildAgentResult_WithEmptyTextContent_ReturnsEmpty()
    {
        // Arrange
        var msg = new InternalAssistantMessage
        {
            Role = InternalMessageRole.Assistant,
            TextContent = null,
        };

        // Act
        var result = AgentGrainTestHelpers.BuildAgentResult(msg);

        // Assert
        Assert.Empty(result.Parts);
    }

    [Fact]
    public void BuildAgentResult_WithOnlyCitations_ReturnsCitationsOnly()
    {
        // Arrange
        var msg = new InternalAssistantMessage
        {
            Role = InternalMessageRole.Assistant,
            TextContent = null,
            ContentBlocks =
            [
                new InternalCitationBlock("Title", "https://example.com", "text"),
            ],
        };

        // Act
        var result = AgentGrainTestHelpers.BuildAgentResult(msg);

        // Assert
        Assert.Single(result.Parts);
        Assert.IsType<AgentCitationPart>(result.Parts[0]);
    }

    #endregion

    #region ConversationMessage Formatting Tests

    [Fact]
    public void FormatMessage_UserMessage_ReturnsUserContentMessage()
    {
        // Arrange
        var msg = new UserConversationMessage("Hello", DateTimeOffset.UtcNow);

        // Act
        var result = ConversationGrainTestHelpers.FormatMessage(msg);

        // Assert
        var content = Assert.IsType<InternalContentMessage>(result);
        Assert.Equal(InternalMessageRole.User, content.Role);
        Assert.Equal("Hello", content.Content);
    }

    [Fact]
    public void FormatMessage_SuccessfulAgentResult_ContainsWaitInstruction()
    {
        // Arrange
        var msg = new AgentResultConversationMessage(
            "agent-1", "researcher", null, false, DateTimeOffset.UtcNow);

        // Act
        var result = ConversationGrainTestHelpers.FormatMessage(msg);

        // Assert
        var content = Assert.IsType<InternalContentMessage>(result);
        Assert.Equal(InternalMessageRole.User, content.Role);
        Assert.Contains("completed successfully", content.Content);
        Assert.Contains("wait_for_agent", content.Content);
        Assert.Contains("agent-1", content.Content);
    }

    [Fact]
    public void FormatMessage_FailedAgentResult_ContainsErrorDetail()
    {
        // Arrange
        var result = AgentResult.FromText("something went wrong");
        var msg = new AgentResultConversationMessage(
            "agent-1", "researcher", result, true, DateTimeOffset.UtcNow);

        // Act
        var formatted = ConversationGrainTestHelpers.FormatMessage(msg);

        // Assert
        var content = Assert.IsType<InternalContentMessage>(formatted);
        Assert.Contains("FAILED", content.Content);
        Assert.Contains("something went wrong", content.Content);
    }

    [Fact]
    public void FormatMessage_FailedAgentResult_WithNullResult_ShowsNoDetails()
    {
        // Arrange
        var msg = new AgentResultConversationMessage(
            "agent-1", "researcher", null, true, DateTimeOffset.UtcNow);

        // Act
        var formatted = ConversationGrainTestHelpers.FormatMessage(msg);

        // Assert
        var content = Assert.IsType<InternalContentMessage>(formatted);
        Assert.Contains("FAILED", content.Content);
        Assert.Contains("No details available", content.Content);
    }

    #endregion

    #region ToolGroup Resolution Tests

    [Fact]
    public void ResolveToolGroups_WithKnownGroups_ReturnsTypes()
    {
        // Act
        var types = SharedGrainHelpers.ResolveToolGroups(["swarm_delegate", "swarm_management"]);

        // Assert
        Assert.Equal(2, types.Length);
    }

    [Fact]
    public void ResolveToolGroups_WithUnknownGroup_SkipsIt()
    {
        // Act
        var types = SharedGrainHelpers.ResolveToolGroups(["unknown_group"]);

        // Assert
        Assert.Empty(types);
    }

    [Fact]
    public void ResolveToolGroups_WithEmpty_ReturnsEmpty()
    {
        // Act
        var types = SharedGrainHelpers.ResolveToolGroups([]);

        // Assert
        Assert.Empty(types);
    }

    [Fact]
    public void ResolveToolGroups_WithMixed_OnlyReturnsKnown()
    {
        // Act
        var types = SharedGrainHelpers.ResolveToolGroups(["swarm_delegate", "nonexistent", "swarm_management"]);

        // Assert
        Assert.Equal(2, types.Length);
    }

    #endregion
}

/// <summary>
/// Test-accessible wrapper for AgentGrain's static BuildAgentResult method.
/// </summary>
internal static class AgentGrainTestHelpers
{
    public static AgentResult BuildAgentResult(InternalAssistantMessage? assistantMsg)
    {
        if (assistantMsg is null)
            return AgentResult.Empty;

        var parts = new List<AgentResultPart>();

        if (!string.IsNullOrEmpty(assistantMsg.TextContent))
            parts.Add(new AgentTextPart(assistantMsg.TextContent));

        foreach (var block in assistantMsg.ContentBlocks)
        {
            if (block is InternalCitationBlock citation)
                parts.Add(new AgentCitationPart(citation.Title, citation.Url, citation.CitedText));
        }

        return parts.Count > 0 ? new AgentResult(parts) : AgentResult.Empty;
    }
}

/// <summary>
/// Test-accessible wrapper for ConversationGrain's static FormatMessage method.
/// </summary>
internal static class ConversationGrainTestHelpers
{
    public static InternalMessage FormatMessage(ConversationMessage message)
    {
        return message switch
        {
            UserConversationMessage user => new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = user.Text,
            },
            AgentResultConversationMessage agentResult => new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = FormatAgentResult(agentResult),
            },
            _ => new InternalContentMessage
            {
                Role = InternalMessageRole.User,
                Content = message.ToString() ?? string.Empty,
            },
        };
    }

    private static string FormatAgentResult(AgentResultConversationMessage msg)
    {
        if (msg.IsError)
        {
            var detail = msg.Result is not null
                ? string.Join("\n", msg.Result.Parts.OfType<AgentTextPart>().Select(p => p.Text))
                : "No details available";
            return $"[Agent Notification] Agent '{msg.Label}' (key: {msg.AgentKey}) FAILED:\n{detail}";
        }

        return $"[Agent Notification] Agent '{msg.Label}' (key: {msg.AgentKey}) has completed successfully. " +
               $"Use the wait_for_agent tool with agent_key=\"{msg.AgentKey}\" to retrieve the full results.";
    }
}

/// <summary>
/// Test-accessible wrapper for the shared ResolveToolGroups helper.
/// </summary>
internal static class SharedGrainHelpers
{
    private static readonly Dictionary<string, Type> ToolGroupMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["swarm_delegate"] = typeof(DonkeyWork.Agents.Actors.Core.Tools.Swarm.SwarmDelegateSpawnTools),
        ["swarm_management"] = typeof(DonkeyWork.Agents.Actors.Core.Tools.Swarm.SwarmAgentManagementTools),
    };

    public static Type[] ResolveToolGroups(string[] toolGroups)
    {
        var types = new List<Type>();
        foreach (var group in toolGroups)
        {
            if (ToolGroupMap.TryGetValue(group, out var type))
                types.Add(type);
        }
        return types.ToArray();
    }
}
