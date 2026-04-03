using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Core.Grains;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Grains;

public class AgentRegistryGrainTests
{
    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithNewAgent_AddsToRegistry()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");

        // Assert
        var agents = await grain.ListAsync();
        Assert.Single(agents);
        Assert.Equal("agent-1", agents[0].AgentKey);
        Assert.Equal("researcher", agents[0].Label);
        Assert.Equal("parent-1", agents[0].ParentAgentKey);
        Assert.Equal(AgentStatus.Pending, agents[0].Status);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicate_IgnoresDuplicate()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");

        // Act
        await grain.RegisterAsync("agent-1", "different-label", "researcher", "parent-1");

        // Assert
        var agents = await grain.ListAsync();
        Assert.Single(agents);
        Assert.Equal("researcher", agents[0].Label); // keeps original
    }

    [Fact]
    public async Task RegisterAsync_MultipleAgents_AddsAll()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        await grain.RegisterAsync("agent-1", "researcher-1", "r1", "parent-1");
        await grain.RegisterAsync("agent-2", "researcher-2", "r2", "parent-1");
        await grain.RegisterAsync("agent-3", "researcher-3", "r3", "parent-1");

        // Assert
        var agents = await grain.ListAsync();
        Assert.Equal(3, agents.Count);
    }

    #endregion

    #region ReportCompletionAsync Tests

    [Fact]
    public async Task ReportCompletionAsync_WithSuccess_UpdatesStatusToCompleted()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");
        var result = AgentResult.FromText("research done");

        // Act
        await grain.ReportCompletionAsync("agent-1", result);

        // Assert
        var agents = await grain.ListAsync();
        Assert.Equal(AgentStatus.Completed, agents[0].Status);
        Assert.NotNull(agents[0].Result);
    }

    [Fact]
    public async Task ReportCompletionAsync_WithError_UpdatesStatusToFailed()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");
        var result = AgentResult.FromText("error occurred");

        // Act
        await grain.ReportCompletionAsync("agent-1", result, isError: true);

        // Assert
        var agents = await grain.ListAsync();
        Assert.Equal(AgentStatus.Failed, agents[0].Status);
    }

    [Fact]
    public async Task ReportCompletionAsync_UnknownAgent_DoesNotThrow()
    {
        // Arrange
        var grain = CreateGrain();
        var result = AgentResult.FromText("result");

        // Act & Assert - should not throw
        await grain.ReportCompletionAsync("unknown", result);
    }

    #endregion

    #region WaitForNextAsync Tests

    [Fact]
    public async Task WaitForNextAsync_WithAlreadyCompleted_ReturnsImmediately()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");
        await grain.ReportCompletionAsync("agent-1", AgentResult.FromText("done"));

        // Act
        var result = await grain.WaitForNextAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("agent-1", result.AgentKey);
        Assert.Equal("researcher", result.Label);
        Assert.Equal(AgentStatus.Completed, result.Status);
    }

    [Fact]
    public async Task WaitForNextAsync_WithNoPending_ReturnsNull()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        var result = await grain.WaitForNextAsync(TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task WaitForNextAsync_WithTimeout_ReturnsNull()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");

        // Act - very short timeout, agent hasn't completed
        var result = await grain.WaitForNextAsync(TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task WaitForNextAsync_CompletedDuringWait_ReturnsResult()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");

        // Act - complete the agent in parallel
        var waitTask = grain.WaitForNextAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(50);
        await grain.ReportCompletionAsync("agent-1", AgentResult.FromText("done"));

        var result = await waitTask;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("agent-1", result.AgentKey);
    }

    [Fact]
    public async Task WaitForNextAsync_DeliveredOnlyOnce()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");
        await grain.ReportCompletionAsync("agent-1", AgentResult.FromText("done"));

        // Act - first wait gets the result
        var first = await grain.WaitForNextAsync(TimeSpan.FromSeconds(1));

        // Second wait with no other pending agents
        var second = await grain.WaitForNextAsync(TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.NotNull(first);
        Assert.Null(second); // already delivered
    }

    #endregion

    #region WaitForSpecificAsync Tests

    [Fact]
    public async Task WaitForSpecificAsync_WithUnknownAgent_ReturnsNull()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        var result = await grain.WaitForSpecificAsync("unknown", TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task WaitForSpecificAsync_AlreadyCompleted_ReturnsImmediately()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");
        await grain.ReportCompletionAsync("agent-1", AgentResult.FromText("done"));

        // Act
        var result = await grain.WaitForSpecificAsync("agent-1", TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("agent-1", result.AgentKey);
        Assert.Equal(AgentStatus.Completed, result.Status);
    }

    [Fact]
    public async Task WaitForSpecificAsync_WithTimeout_ReturnsNull()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");

        // Act
        var result = await grain.WaitForSpecificAsync("agent-1", TimeSpan.FromMilliseconds(50));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task WaitForSpecificAsync_CompletedDuringWait_ReturnsResult()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");

        // Act
        var waitTask = grain.WaitForSpecificAsync("agent-1", TimeSpan.FromSeconds(5));
        await Task.Delay(50);
        await grain.ReportCompletionAsync("agent-1", AgentResult.FromText("done"));

        var result = await waitTask;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("agent-1", result.AgentKey);
    }

    [Fact]
    public async Task WaitForSpecificAsync_FailedAgent_ReturnsErrorResult()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "researcher", "researcher", "parent-1");
        await grain.ReportCompletionAsync("agent-1", AgentResult.FromText("error"), isError: true);

        // Act
        var result = await grain.WaitForSpecificAsync("agent-1", TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AgentStatus.Failed, result.Status);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_EmptyRegistry_ReturnsEmptyList()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        var agents = await grain.ListAsync();

        // Assert
        Assert.Empty(agents);
    }

    [Fact]
    public async Task ListAsync_OrdersBySpawnTime()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "first", "first", "parent-1");
        await Task.Delay(10);
        await grain.RegisterAsync("agent-2", "second", "second", "parent-1");

        // Act
        var agents = await grain.ListAsync();

        // Assert
        Assert.Equal(2, agents.Count);
        Assert.Equal("agent-1", agents[0].AgentKey);
        Assert.Equal("agent-2", agents[1].AgentKey);
    }

    #endregion

    #region Named Addressing Tests

    [Fact]
    public async Task RegisterAsync_AssignsUniqueName()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        var name = await grain.RegisterAsync("agent-1", "research task", "researcher", "parent-1");

        // Assert
        Assert.Equal("researcher", name);
        var agents = await grain.ListAsync();
        Assert.Equal("researcher", agents[0].Name);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateNames_GetsSuffix()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        var name1 = await grain.RegisterAsync("agent-1", "task 1", "researcher", "parent-1");
        var name2 = await grain.RegisterAsync("agent-2", "task 2", "researcher", "parent-1");
        var name3 = await grain.RegisterAsync("agent-3", "task 3", "researcher", "parent-1");

        // Assert
        Assert.Equal("researcher", name1);
        Assert.Equal("researcher_2", name2);
        Assert.Equal("researcher_3", name3);
    }

    [Fact]
    public async Task ResolveAgentKeyByNameAsync_KnownName_ReturnsKey()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");

        // Act
        var key = await grain.ResolveAgentKeyByNameAsync("researcher");

        // Assert
        Assert.Equal("agent-1", key);
    }

    [Fact]
    public async Task ResolveAgentKeyByNameAsync_UnknownName_ReturnsNull()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        var key = await grain.ResolveAgentKeyByNameAsync("unknown");

        // Assert
        Assert.Null(key);
    }

    [Fact]
    public async Task ResolveAgentKeyByNameAsync_CaseInsensitive()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "Researcher", "parent-1");

        // Act
        var key = await grain.ResolveAgentKeyByNameAsync("researcher");

        // Assert
        Assert.Equal("agent-1", key);
    }

    #endregion

    #region ReportIdleAsync Tests

    [Fact]
    public async Task ReportIdleAsync_SetsStatusToIdle()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");

        // Act
        await grain.ReportIdleAsync("agent-1");

        // Assert
        var agents = await grain.ListAsync();
        Assert.Equal(AgentStatus.Idle, agents[0].Status);
    }

    [Fact]
    public async Task ReportIdleAsync_UnknownAgent_DoesNotThrow()
    {
        // Arrange
        var grain = CreateGrain();

        // Act & Assert
        await grain.ReportIdleAsync("unknown");
    }

    [Fact]
    public async Task ReportIdleAsync_WithResult_SetsResultOnTrackedAgent()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");
        var result = AgentResult.FromText("partial findings");

        // Act
        await grain.ReportIdleAsync("agent-1", result);

        // Assert
        var agents = await grain.ListAsync();
        Assert.NotNull(agents[0].Result);
        var textPart = Assert.IsType<AgentTextPart>(agents[0].Result!.Parts[0]);
        Assert.Equal("partial findings", textPart.Text);
    }

    [Fact]
    public async Task ReportIdleAsync_WithResult_WaitForSpecificReturnsActualResult()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");
        var result = AgentResult.FromText("partial findings");

        // Act
        await grain.ReportIdleAsync("agent-1", result);
        var waitResult = await grain.WaitForSpecificAsync("agent-1", TimeSpan.FromSeconds(1));

        // Assert
        Assert.NotNull(waitResult);
        var textPart = Assert.IsType<AgentTextPart>(waitResult.Result.Parts[0]);
        Assert.Equal("partial findings", textPart.Text);
    }

    [Fact]
    public async Task ReportIdleAsync_WithoutResult_WaitForSpecificReturnsEmptyResult()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");

        // Act
        await grain.ReportIdleAsync("agent-1");
        var waitResult = await grain.WaitForSpecificAsync("agent-1", TimeSpan.FromSeconds(1));

        // Assert
        Assert.NotNull(waitResult);
        Assert.Empty(waitResult.Result.Parts);
    }

    #endregion

    #region ReportResumedAsync Tests

    [Fact]
    public async Task ReportResumedAsync_ResetsStatusToPending()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");
        await grain.ReportIdleAsync("agent-1");

        // Act
        await grain.ReportResumedAsync("agent-1");

        // Assert
        var agents = await grain.ListAsync();
        Assert.Equal(AgentStatus.Pending, agents[0].Status);
    }

    [Fact]
    public async Task ReportResumedAsync_ClearsResult()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");
        await grain.ReportIdleAsync("agent-1", AgentResult.FromText("old data"));

        // Act
        await grain.ReportResumedAsync("agent-1");

        // Assert
        var agents = await grain.ListAsync();
        Assert.Null(agents[0].Result);
    }

    [Fact]
    public async Task ReportResumedAsync_ResetsDelivered_AllowsRedelivery()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");
        await grain.ReportIdleAsync("agent-1");
        await grain.WaitForNextAsync(TimeSpan.FromSeconds(1)); // marks as delivered

        // Act
        await grain.ReportResumedAsync("agent-1");
        await grain.ReportIdleAsync("agent-1", AgentResult.FromText("new data"));
        var result = await grain.WaitForNextAsync(TimeSpan.FromSeconds(1));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("agent-1", result.AgentKey);
    }

    [Fact]
    public async Task ReportResumedAsync_CreatesNewTcs_SubsequentIdleReturnsNewResult()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");

        // First idle/wait cycle
        await grain.ReportIdleAsync("agent-1", AgentResult.FromText("result-1"));
        var first = await grain.WaitForSpecificAsync("agent-1", TimeSpan.FromSeconds(1));
        Assert.NotNull(first);
        var firstText = Assert.IsType<AgentTextPart>(first.Result.Parts[0]);
        Assert.Equal("result-1", firstText.Text);

        // Act - resume then idle with different result
        await grain.ReportResumedAsync("agent-1");
        await grain.ReportIdleAsync("agent-1", AgentResult.FromText("result-2"));
        var second = await grain.WaitForSpecificAsync("agent-1", TimeSpan.FromSeconds(1));

        // Assert
        Assert.NotNull(second);
        var secondText = Assert.IsType<AgentTextPart>(second.Result.Parts[0]);
        Assert.Equal("result-2", secondText.Text);
    }

    [Fact]
    public async Task ReportResumedAsync_FullRoundTrip()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");

        // Round 1: idle with result1 -> wait gets result1
        await grain.ReportIdleAsync("agent-1", AgentResult.FromText("first"));
        var wait1 = await grain.WaitForSpecificAsync("agent-1", TimeSpan.FromSeconds(1));
        Assert.NotNull(wait1);
        Assert.Equal(AgentStatus.Idle, wait1.Status);
        var text1 = Assert.IsType<AgentTextPart>(wait1.Result.Parts[0]);
        Assert.Equal("first", text1.Text);

        // Round 2: resume -> idle with result2 -> wait gets result2
        await grain.ReportResumedAsync("agent-1");
        await grain.ReportIdleAsync("agent-1", AgentResult.FromText("second"));
        var wait2 = await grain.WaitForSpecificAsync("agent-1", TimeSpan.FromSeconds(1));
        Assert.NotNull(wait2);
        Assert.Equal(AgentStatus.Idle, wait2.Status);
        var text2 = Assert.IsType<AgentTextPart>(wait2.Result.Parts[0]);
        Assert.Equal("second", text2.Text);
    }

    [Fact]
    public async Task ReportResumedAsync_UnknownAgent_DoesNotThrow()
    {
        // Arrange
        var grain = CreateGrain();

        // Act & Assert
        await grain.ReportResumedAsync("unknown");
    }

    #endregion

    #region ReportExpiredAsync Tests

    [Fact]
    public async Task ReportExpiredAsync_SetsStatusToExpired()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");

        // Act
        await grain.ReportExpiredAsync("agent-1");

        // Assert
        var agents = await grain.ListAsync();
        Assert.Equal(AgentStatus.Expired, agents[0].Status);
    }

    [Fact]
    public async Task ReportExpiredAsync_StampsCollectedAt()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");
        var before = DateTimeOffset.UtcNow;

        // Act
        await grain.ReportExpiredAsync("agent-1");

        // Assert
        var collectedAt = grain.GetCollectedAt("agent-1");
        Assert.NotNull(collectedAt);
        Assert.True(collectedAt.Value >= before);
    }

    [Fact]
    public async Task ReportExpiredAsync_UnknownAgent_DoesNotThrow()
    {
        // Arrange
        var grain = CreateGrain();

        // Act & Assert
        await grain.ReportExpiredAsync("unknown");
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public async Task RunCleanupAsync_RemovesEntriesOlderThanGracePeriod()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");
        await grain.ReportExpiredAsync("agent-1");
        grain.SetCollectedAt("agent-1", DateTimeOffset.UtcNow.AddMinutes(-6));

        // Act
        await grain.RunCleanupAsync();

        // Assert
        var agents = await grain.ListAsync();
        Assert.Empty(agents);
    }

    [Fact]
    public async Task RunCleanupAsync_KeepsEntriesWithinGracePeriod()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");
        await grain.ReportExpiredAsync("agent-1");
        grain.SetCollectedAt("agent-1", DateTimeOffset.UtcNow.AddMinutes(-3));

        // Act
        await grain.RunCleanupAsync();

        // Assert
        var agents = await grain.ListAsync();
        Assert.Single(agents);
    }

    [Fact]
    public async Task RunCleanupAsync_DoesNotRemoveIdleAgentsWithoutCollectedAt()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");
        await grain.ReportIdleAsync("agent-1");

        // Act
        await grain.RunCleanupAsync();

        // Assert
        var agents = await grain.ListAsync();
        Assert.Single(agents);
    }

    [Fact]
    public async Task RunCleanupAsync_DoesNotRemovePendingAgents()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");

        // Act
        await grain.RunCleanupAsync();

        // Assert
        var agents = await grain.ListAsync();
        Assert.Single(agents);
    }

    [Fact]
    public async Task RunCleanupAsync_RemovesFromNameIndex()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "task", "researcher", "parent-1");
        await grain.ReportExpiredAsync("agent-1");
        grain.SetCollectedAt("agent-1", DateTimeOffset.UtcNow.AddMinutes(-6));

        // Act
        await grain.RunCleanupAsync();

        // Assert
        var key = await grain.ResolveAgentKeyByNameAsync("researcher");
        Assert.Null(key);
    }

    [Fact]
    public async Task RunCleanupAsync_MixedEntries_OnlyRemovesExpired()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("agent-1", "old-expired", "researcher", "parent-1");
        await grain.RegisterAsync("agent-2", "fresh-expired", "writer", "parent-1");
        await grain.RegisterAsync("agent-3", "still-pending", "coder", "parent-1");

        await grain.ReportExpiredAsync("agent-1");
        grain.SetCollectedAt("agent-1", DateTimeOffset.UtcNow.AddMinutes(-10));

        await grain.ReportExpiredAsync("agent-2");
        grain.SetCollectedAt("agent-2", DateTimeOffset.UtcNow.AddMinutes(-2));

        // Act
        await grain.RunCleanupAsync();

        // Assert
        var agents = await grain.ListAsync();
        Assert.Equal(2, agents.Count);
        Assert.Contains(agents, a => a.AgentKey == "agent-2");
        Assert.Contains(agents, a => a.AgentKey == "agent-3");
    }

    #endregion

    #region ListScopedAsync Tests

    [Fact]
    public async Task ListScopedAsync_UnknownAgent_ReturnsEmpty()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        var result = await grain.ListScopedAsync("unknown");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListScopedAsync_RootAgent_ReturnsSelfAndDirectChildren()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("root", "orchestrator", "orchestrator", "root"); // root: parent == self
        await grain.RegisterAsync("child-1", "researcher", "researcher", "root");
        await grain.RegisterAsync("child-2", "writer", "writer", "root");

        // Act
        var result = await grain.ListScopedAsync("root");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, a => a.AgentKey == "root");
        Assert.Contains(result, a => a.AgentKey == "child-1");
        Assert.Contains(result, a => a.AgentKey == "child-2");
    }

    [Fact]
    public async Task ListScopedAsync_ChildAgent_ReturnsSelfParentAndDescendants()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("root", "orchestrator", "orchestrator", "root");
        await grain.RegisterAsync("child", "researcher", "researcher", "root");
        await grain.RegisterAsync("grandchild", "sub-task", "subtask", "child");

        // Act
        var result = await grain.ListScopedAsync("child");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, a => a.AgentKey == "root");
        Assert.Contains(result, a => a.AgentKey == "child");
        Assert.Contains(result, a => a.AgentKey == "grandchild");
    }

    [Fact]
    public async Task ListScopedAsync_DoesNotReturnSiblings()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("root", "orchestrator", "orchestrator", "root");
        await grain.RegisterAsync("child-1", "researcher", "researcher", "root");
        await grain.RegisterAsync("child-2", "writer", "writer", "root");

        // Act
        var result = await grain.ListScopedAsync("child-1");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.AgentKey == "root");
        Assert.Contains(result, a => a.AgentKey == "child-1");
        Assert.DoesNotContain(result, a => a.AgentKey == "child-2");
    }

    [Fact]
    public async Task ListScopedAsync_LeafAgent_ReturnsSelfAndParent()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("root", "orchestrator", "orchestrator", "root");
        await grain.RegisterAsync("child", "researcher", "researcher", "root");
        await grain.RegisterAsync("leaf", "sub-task", "subtask", "child");

        // Act
        var result = await grain.ListScopedAsync("leaf");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.AgentKey == "child");
        Assert.Contains(result, a => a.AgentKey == "leaf");
    }

    [Fact]
    public async Task ListScopedAsync_IncludesDeepDescendants()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("root", "orchestrator", "orchestrator", "root");
        await grain.RegisterAsync("child", "researcher", "researcher", "root");
        await grain.RegisterAsync("grandchild", "sub-researcher", "sub", "child");
        await grain.RegisterAsync("great-grandchild", "detail", "detail", "grandchild");

        // Act
        var result = await grain.ListScopedAsync("child");

        // Assert
        Assert.Equal(4, result.Count);
        Assert.Contains(result, a => a.AgentKey == "root");
        Assert.Contains(result, a => a.AgentKey == "child");
        Assert.Contains(result, a => a.AgentKey == "grandchild");
        Assert.Contains(result, a => a.AgentKey == "great-grandchild");
    }

    #endregion

    #region GetScopedRosterAsync Tests

    [Fact]
    public async Task GetScopedRosterAsync_NoAgents_ReturnsEmptyString()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        var roster = await grain.GetScopedRosterAsync("agent-1");

        // Assert
        Assert.Equal(string.Empty, roster);
    }

    [Fact]
    public async Task GetScopedRosterAsync_ContainsYouMarkerForSelf()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("root", "orchestrator", "orchestrator", "root");

        // Act
        var roster = await grain.GetScopedRosterAsync("root");

        // Assert
        Assert.Contains("(you)", roster);
    }

    [Fact]
    public async Task GetScopedRosterAsync_DoesNotMarkOtherAgentsAsYou()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("root", "orchestrator", "orchestrator", "root");
        await grain.RegisterAsync("child", "researcher", "researcher", "root");

        // Act
        var roster = await grain.GetScopedRosterAsync("root");

        // Assert
        var lines = roster.Split('\n');
        var childLine = Array.Find(lines, l => l.Contains("researcher") && !l.Contains("orchestrator"));
        Assert.NotNull(childLine);
        Assert.DoesNotContain("(you)", childLine);
    }

    [Fact]
    public async Task GetScopedRosterAsync_ContainsAgentNames()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("root", "orchestrator task", "orchestrator", "root");
        await grain.RegisterAsync("child", "research task", "researcher", "root");

        // Act
        var roster = await grain.GetScopedRosterAsync("root");

        // Assert
        Assert.Contains("orchestrator", roster);
        Assert.Contains("researcher", roster);
    }

    [Fact]
    public async Task GetScopedRosterAsync_ContainsAgentStatus()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("root", "task", "orchestrator", "root");
        await grain.RegisterAsync("child", "task", "researcher", "root");
        await grain.ReportIdleAsync("child");

        // Act
        var roster = await grain.GetScopedRosterAsync("root");

        // Assert
        Assert.Contains("pending", roster);
        Assert.Contains("idle", roster);
    }

    [Fact]
    public async Task GetScopedRosterAsync_ShowsIndentationForNestedAgents()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.RegisterAsync("root", "orchestrator", "orchestrator", "root");
        await grain.RegisterAsync("child", "researcher", "researcher", "root");
        await grain.RegisterAsync("grandchild", "sub-task", "subtask", "child");

        // Act
        var roster = await grain.GetScopedRosterAsync("root");

        // Assert
        var lines = roster.Split('\n');
        var childLine = Array.Find(lines, l => l.Contains("researcher") && !l.Contains("subtask"));
        var grandchildLine = Array.Find(lines, l => l.Contains("subtask"));
        Assert.NotNull(childLine);
        Assert.NotNull(grandchildLine);
        Assert.StartsWith("  ", childLine);
        Assert.StartsWith("    ", grandchildLine);
    }

    #endregion

    #region Shared Context Tests

    [Fact]
    public async Task WriteAndReadSharedContext_RoundTrips()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        await grain.WriteSharedContextAsync("findings", "important data");
        var value = await grain.ReadSharedContextAsync("findings");

        // Assert
        Assert.Equal("important data", value);
    }

    [Fact]
    public async Task ReadSharedContext_MissingKey_ReturnsNull()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        var value = await grain.ReadSharedContextAsync("missing");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public async Task ReadAllSharedContext_ReturnsAllEntries()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.WriteSharedContextAsync("key1", "value1");
        await grain.WriteSharedContextAsync("key2", "value2");

        // Act
        var all = await grain.ReadAllSharedContextAsync();

        // Assert
        Assert.Equal(2, all.Count);
        Assert.Equal("value1", all["key1"]);
        Assert.Equal("value2", all["key2"]);
    }

    [Fact]
    public async Task WriteSharedContext_OverwritesExisting()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.WriteSharedContextAsync("key", "original");

        // Act
        await grain.WriteSharedContextAsync("key", "updated");
        var value = await grain.ReadSharedContextAsync("key");

        // Assert
        Assert.Equal("updated", value);
    }

    [Fact]
    public async Task RemoveSharedContext_RemovesEntry()
    {
        // Arrange
        var grain = CreateGrain();
        await grain.WriteSharedContextAsync("key", "value");

        // Act
        var removed = await grain.RemoveSharedContextAsync("key");
        var value = await grain.ReadSharedContextAsync("key");

        // Assert
        Assert.True(removed);
        Assert.Null(value);
    }

    [Fact]
    public async Task RemoveSharedContext_MissingKey_ReturnsFalse()
    {
        // Arrange
        var grain = CreateGrain();

        // Act
        var removed = await grain.RemoveSharedContextAsync("missing");

        // Assert
        Assert.False(removed);
    }

    #endregion

    #region Helpers

    private static TestableAgentRegistryGrain CreateGrain()
    {
        return new TestableAgentRegistryGrain(NullLogger<AgentRegistryGrain>.Instance);
    }

    private sealed class TestableAgentRegistryGrain
    {
        private readonly AgentRegistryGrainLogic _logic;

        public TestableAgentRegistryGrain(Microsoft.Extensions.Logging.ILogger<AgentRegistryGrain> logger)
        {
            _logic = new AgentRegistryGrainLogic(logger);
        }

        public Task<string> RegisterAsync(string agentKey, string label, string name, string parentAgentKey, TimeSpan? timeout = null)
            => _logic.RegisterAsync(agentKey, label, name, parentAgentKey, timeout);

        public Task ReportCompletionAsync(string agentKey, AgentResult result, bool isError = false)
            => _logic.ReportCompletionAsync(agentKey, result, isError);

        public Task<AgentWaitResult?> WaitForNextAsync(TimeSpan? timeout = null)
            => _logic.WaitForNextAsync(timeout);

        public Task<AgentWaitResult?> WaitForSpecificAsync(string agentKey, TimeSpan? timeout = null)
            => _logic.WaitForSpecificAsync(agentKey, timeout);

        public Task<IReadOnlyList<TrackedAgent>> ListAsync()
            => _logic.ListAsync();

        public Task<string?> ResolveAgentKeyByNameAsync(string name)
            => _logic.ResolveAgentKeyByNameAsync(name);

        public Task ReportIdleAsync(string agentKey, AgentResult? result = null)
            => _logic.ReportIdleAsync(agentKey, result);

        public Task ReportResumedAsync(string agentKey)
            => _logic.ReportResumedAsync(agentKey);

        public Task ReportExpiredAsync(string agentKey)
            => _logic.ReportExpiredAsync(agentKey);

        public Task<IReadOnlyList<TrackedAgent>> ListScopedAsync(string agentKey)
            => _logic.ListScopedAsync(agentKey);

        public Task<string> GetScopedRosterAsync(string agentKey)
            => _logic.GetScopedRosterAsync(agentKey);

        public Task RunCleanupAsync()
            => _logic.RunCleanupAsync();

        public DateTimeOffset? GetCollectedAt(string agentKey)
            => _logic.GetCollectedAt(agentKey);

        public void SetCollectedAt(string agentKey, DateTimeOffset value)
            => _logic.SetCollectedAt(agentKey, value);

        public Task WriteSharedContextAsync(string key, string value)
            => _logic.WriteSharedContextAsync(key, value);

        public Task<string?> ReadSharedContextAsync(string key)
            => _logic.ReadSharedContextAsync(key);

        public Task<IReadOnlyDictionary<string, string>> ReadAllSharedContextAsync()
            => _logic.ReadAllSharedContextAsync();

        public Task<bool> RemoveSharedContextAsync(string key)
            => _logic.RemoveSharedContextAsync(key);
    }

    private sealed class AgentRegistryGrainLogic
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly Dictionary<string, AgentEntry> _agents = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _nameIndex = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _nameCounters = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _sharedContext = new(StringComparer.OrdinalIgnoreCase);

        public AgentRegistryGrainLogic(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger;
        }

        public Task<string> RegisterAsync(string agentKey, string label, string name, string parentAgentKey, TimeSpan? timeout = null)
        {
            if (_agents.ContainsKey(agentKey))
                return Task.FromResult(_agents[agentKey].Info.Name);

            var uniqueName = AssignUniqueName(name);

            _agents[agentKey] = new AgentEntry
            {
                Info = new TrackedAgent(agentKey, label, parentAgentKey, AgentStatus.Pending, null, DateTime.UtcNow, uniqueName),
            };
            _nameIndex[uniqueName] = agentKey;
            return Task.FromResult(uniqueName);
        }

        public Task ReportCompletionAsync(string agentKey, AgentResult result, bool isError = false)
        {
            if (!_agents.TryGetValue(agentKey, out var entry))
                return Task.CompletedTask;

            var newStatus = isError ? AgentStatus.Failed : AgentStatus.Completed;
            entry.Info = entry.Info with { Status = newStatus, Result = result };

            if (isError)
            {
                var errorText = result.Parts.OfType<AgentTextPart>().FirstOrDefault()?.Text ?? "Agent failed";
                entry.Tcs.TrySetException(new InvalidOperationException(errorText));
            }
            else
            {
                entry.Tcs.TrySetResult(result);
            }

            return Task.CompletedTask;
        }

        public Task ReportIdleAsync(string agentKey, AgentResult? result = null)
        {
            if (_agents.TryGetValue(agentKey, out var entry))
            {
                entry.Info = entry.Info with { Status = AgentStatus.Idle, Result = result };
                entry.Tcs.TrySetResult(result ?? AgentResult.Empty);
            }
            return Task.CompletedTask;
        }

        public Task ReportResumedAsync(string agentKey)
        {
            if (_agents.TryGetValue(agentKey, out var entry))
            {
                entry.Info = entry.Info with { Status = AgentStatus.Pending, Result = null };
                entry.Tcs = new TaskCompletionSource<AgentResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                entry.Delivered = false;
                entry.CollectedAt = null;
            }
            return Task.CompletedTask;
        }

        public Task ReportExpiredAsync(string agentKey)
        {
            if (_agents.TryGetValue(agentKey, out var entry))
            {
                entry.Info = entry.Info with { Status = AgentStatus.Expired };
                entry.CollectedAt = DateTimeOffset.UtcNow;
                entry.Tcs.TrySetResult(entry.Info.Result ?? AgentResult.Empty);
            }
            return Task.CompletedTask;
        }

        public async Task<AgentWaitResult?> WaitForNextAsync(TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromSeconds(30);

            var alreadyDone = _agents.Values
                .Where(e => !e.Delivered && e.Info.Status is AgentStatus.Completed or AgentStatus.Failed or AgentStatus.Idle)
                .OrderBy(e => e.Info.SpawnedAt)
                .FirstOrDefault();

            if (alreadyDone is not null)
            {
                alreadyDone.Delivered = true;
                return BuildWaitResult(alreadyDone);
            }

            var pendingTasks = _agents.Values
                .Where(e => e.Info.Status == AgentStatus.Pending)
                .Select(e => e.Tcs.Task)
                .ToList();

            if (pendingTasks.Count == 0)
                return null;

            var timeoutTask = Task.Delay(timeout.Value);
            var completedTask = await Task.WhenAny([.. pendingTasks, timeoutTask]);

            if (completedTask == timeoutTask)
                return null;

            var completed = _agents.Values
                .Where(e => !e.Delivered && e.Info.Status is AgentStatus.Completed or AgentStatus.Failed or AgentStatus.Idle)
                .OrderBy(e => e.Info.SpawnedAt)
                .FirstOrDefault();

            if (completed is null)
                return null;

            completed.Delivered = true;
            return BuildWaitResult(completed);
        }

        public async Task<AgentWaitResult?> WaitForSpecificAsync(string agentKey, TimeSpan? timeout = null)
        {
            if (!_agents.TryGetValue(agentKey, out var entry))
                return null;

            if (entry.Info.Status is AgentStatus.Completed or AgentStatus.Failed or AgentStatus.Idle)
                return BuildWaitResult(entry);

            timeout ??= TimeSpan.FromSeconds(30);

            var timeoutTask = Task.Delay(timeout.Value);
            var completedTask = await Task.WhenAny(entry.Tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
                return null;

            return BuildWaitResult(entry);
        }

        public Task<IReadOnlyList<TrackedAgent>> ListAsync()
        {
            var agents = _agents.Values
                .OrderBy(e => e.Info.SpawnedAt)
                .Select(e => e.Info)
                .ToList();
            return Task.FromResult<IReadOnlyList<TrackedAgent>>(agents);
        }

        public Task<string?> ResolveAgentKeyByNameAsync(string name)
        {
            _nameIndex.TryGetValue(name, out var agentKey);
            return Task.FromResult(agentKey);
        }

        public Task<IReadOnlyList<TrackedAgent>> ListScopedAsync(string agentKey)
        {
            var visible = GetVisibleAgents(agentKey);
            return Task.FromResult<IReadOnlyList<TrackedAgent>>(visible);
        }

        public Task<string> GetScopedRosterAsync(string agentKey)
        {
            var allEntries = _agents.Values.Select(e => e.Info).ToList();
            if (allEntries.Count == 0)
                return Task.FromResult(string.Empty);

            var roster = BuildRosterTree(agentKey, allEntries);
            return Task.FromResult(roster);
        }

        public Task RunCleanupAsync()
        {
            var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5);
            var toRemove = _agents
                .Where(kv => kv.Value.CollectedAt is not null && kv.Value.CollectedAt.Value < cutoff)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                if (_agents.TryGetValue(key, out var entry))
                {
                    _nameIndex.Remove(entry.Info.Name);
                    _agents.Remove(key);
                }
            }

            return Task.CompletedTask;
        }

        public DateTimeOffset? GetCollectedAt(string agentKey)
        {
            return _agents.TryGetValue(agentKey, out var entry) ? entry.CollectedAt : null;
        }

        public void SetCollectedAt(string agentKey, DateTimeOffset value)
        {
            if (_agents.TryGetValue(agentKey, out var entry))
                entry.CollectedAt = value;
        }

        public Task WriteSharedContextAsync(string key, string value)
        {
            _sharedContext[key] = value;
            return Task.CompletedTask;
        }

        public Task<string?> ReadSharedContextAsync(string key)
        {
            _sharedContext.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task<IReadOnlyDictionary<string, string>> ReadAllSharedContextAsync()
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(_sharedContext, StringComparer.OrdinalIgnoreCase));
        }

        public Task<bool> RemoveSharedContextAsync(string key)
        {
            return Task.FromResult(_sharedContext.Remove(key));
        }

        private List<TrackedAgent> GetVisibleAgents(string agentKey)
        {
            if (!_agents.TryGetValue(agentKey, out var self))
                return [];

            var visible = new List<TrackedAgent>();

            if (_agents.TryGetValue(self.Info.ParentAgentKey, out var parent)
                && parent.Info.AgentKey != agentKey)
            {
                visible.Add(parent.Info);
            }

            visible.Add(self.Info);

            AddDescendants(agentKey, visible);

            return visible;
        }

        private void AddDescendants(string parentKey, List<TrackedAgent> result)
        {
            foreach (var entry in _agents.Values)
            {
                if (string.Equals(entry.Info.ParentAgentKey, parentKey, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(entry.Info.AgentKey, parentKey, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(entry.Info);
                    AddDescendants(entry.Info.AgentKey, result);
                }
            }
        }

        private static string BuildRosterTree(string selfKey, List<TrackedAgent> allAgents)
        {
            var byParent = allAgents
                .Where(a => !string.Equals(a.AgentKey, a.ParentAgentKey, StringComparison.OrdinalIgnoreCase))
                .GroupBy(a => a.ParentAgentKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.SpawnedAt).ToList(), StringComparer.OrdinalIgnoreCase);

            var roots = allAgents
                .Where(a => string.Equals(a.AgentKey, a.ParentAgentKey, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.SpawnedAt)
                .ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\n\n## Swarm");

            foreach (var root in roots)
            {
                RenderNode(sb, root, selfKey, byParent, 0);
            }

            return sb.ToString();
        }

        private static void RenderNode(
            System.Text.StringBuilder sb,
            TrackedAgent agent,
            string selfKey,
            Dictionary<string, List<TrackedAgent>> byParent,
            int depth)
        {
            var indent = new string(' ', depth * 2);
            var youMarker = string.Equals(agent.AgentKey, selfKey, StringComparison.OrdinalIgnoreCase) ? " (you)" : "";
            var status = agent.Status.ToString().ToLowerInvariant();
            var label = !string.IsNullOrEmpty(agent.Label) ? $" — \"{agent.Label}\"" : "";

            sb.AppendLine($"{indent}- {agent.Name} ({status}{youMarker}){label}");

            if (byParent.TryGetValue(agent.AgentKey, out var children))
            {
                foreach (var child in children)
                {
                    RenderNode(sb, child, selfKey, byParent, depth + 1);
                }
            }
        }

        private string AssignUniqueName(string baseName)
        {
            if (!_nameCounters.TryGetValue(baseName, out var count))
            {
                _nameCounters[baseName] = 1;
                return baseName;
            }

            var next = count + 1;
            _nameCounters[baseName] = next;
            return $"{baseName}_{next}";
        }

        private static AgentWaitResult BuildWaitResult(AgentEntry entry)
        {
            AgentResult result;
            if (entry.Tcs.Task.IsCompletedSuccessfully)
                result = entry.Tcs.Task.Result;
            else if (entry.Tcs.Task.IsFaulted)
                result = AgentResult.FromText(entry.Tcs.Task.Exception?.InnerException?.Message ?? "Agent failed");
            else
                result = entry.Info.Result ?? AgentResult.Empty;

            return new AgentWaitResult(entry.Info.AgentKey, entry.Info.Label, result, entry.Info.Status);
        }

        private sealed class AgentEntry
        {
            public TrackedAgent Info { get; set; } = null!;
            public TaskCompletionSource<AgentResult> Tcs { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public bool Delivered { get; set; }
            public DateTimeOffset? CollectedAt { get; set; }
        }
    }

    #endregion
}
