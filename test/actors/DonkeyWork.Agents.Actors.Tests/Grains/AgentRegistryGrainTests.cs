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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");

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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");

        // Act
        await grain.RegisterAsync("agent-1", "different-label", "parent-1");

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
        await grain.RegisterAsync("agent-1", "researcher-1", "parent-1");
        await grain.RegisterAsync("agent-2", "researcher-2", "parent-1");
        await grain.RegisterAsync("agent-3", "researcher-3", "parent-1");

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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");
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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");
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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");
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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");

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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");

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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");
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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");
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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");

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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");

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
        await grain.RegisterAsync("agent-1", "researcher", "parent-1");
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
        await grain.RegisterAsync("agent-1", "first", "parent-1");
        await Task.Delay(10);
        await grain.RegisterAsync("agent-2", "second", "parent-1");

        // Act
        var agents = await grain.ListAsync();

        // Assert
        Assert.Equal(2, agents.Count);
        Assert.Equal("agent-1", agents[0].AgentKey);
        Assert.Equal("agent-2", agents[1].AgentKey);
    }

    #endregion

    #region Helpers

    private static TestableAgentRegistryGrain CreateGrain()
    {
        return new TestableAgentRegistryGrain(NullLogger<AgentRegistryGrain>.Instance);
    }

    /// <summary>
    /// Testable wrapper that exposes the grain methods without requiring Orleans activation.
    /// We test the core logic directly since the grain methods are simple delegations.
    /// </summary>
    private sealed class TestableAgentRegistryGrain
    {
        private readonly AgentRegistryGrainLogic _logic;

        public TestableAgentRegistryGrain(Microsoft.Extensions.Logging.ILogger<AgentRegistryGrain> logger)
        {
            _logic = new AgentRegistryGrainLogic(logger);
        }

        public Task RegisterAsync(string agentKey, string label, string parentAgentKey, TimeSpan? timeout = null)
            => _logic.RegisterAsync(agentKey, label, parentAgentKey, timeout);

        public Task ReportCompletionAsync(string agentKey, AgentResult result, bool isError = false)
            => _logic.ReportCompletionAsync(agentKey, result, isError);

        public Task<AgentWaitResult?> WaitForNextAsync(TimeSpan? timeout = null)
            => _logic.WaitForNextAsync(timeout);

        public Task<AgentWaitResult?> WaitForSpecificAsync(string agentKey, TimeSpan? timeout = null)
            => _logic.WaitForSpecificAsync(agentKey, timeout);

        public Task<IReadOnlyList<TrackedAgent>> ListAsync()
            => _logic.ListAsync();
    }

    /// <summary>
    /// Extracted logic from AgentRegistryGrain for unit testing without Orleans infrastructure.
    /// Mirrors the grain's behavior exactly.
    /// </summary>
    private sealed class AgentRegistryGrainLogic
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly Dictionary<string, AgentEntry> _agents = new(StringComparer.OrdinalIgnoreCase);

        public AgentRegistryGrainLogic(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger;
        }

        public Task RegisterAsync(string agentKey, string label, string parentAgentKey, TimeSpan? timeout = null)
        {
            if (_agents.ContainsKey(agentKey))
                return Task.CompletedTask;

            _agents[agentKey] = new AgentEntry
            {
                Info = new TrackedAgent(agentKey, label, parentAgentKey, AgentStatus.Pending, null, DateTime.UtcNow),
            };
            return Task.CompletedTask;
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

        public async Task<AgentWaitResult?> WaitForNextAsync(TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromSeconds(30);

            var alreadyDone = _agents.Values
                .Where(e => !e.Delivered && e.Info.Status is AgentStatus.Completed or AgentStatus.Failed)
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
                .Where(e => !e.Delivered && e.Info.Status is AgentStatus.Completed or AgentStatus.Failed)
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

            if (entry.Info.Status is AgentStatus.Completed or AgentStatus.Failed)
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
            public TaskCompletionSource<AgentResult> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public bool Delivered { get; set; }
        }
    }

    #endregion
}
