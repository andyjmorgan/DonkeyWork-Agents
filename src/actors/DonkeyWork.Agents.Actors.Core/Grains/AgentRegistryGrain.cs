using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace DonkeyWork.Agents.Actors.Core.Grains;

[Reentrant]
public sealed class AgentRegistryGrain : Grain, IAgentRegistryGrain
{
    private readonly ILogger<AgentRegistryGrain> _logger;
    private readonly Dictionary<string, AgentEntry> _agents = new(StringComparer.OrdinalIgnoreCase);

    public AgentRegistryGrain(ILogger<AgentRegistryGrain> logger)
    {
        _logger = logger;
    }

    #region IAgentRegistryGrain

    public Task RegisterAsync(string agentKey, string label, string parentAgentKey, TimeSpan? timeout = null)
    {
        if (_agents.ContainsKey(agentKey))
        {
            _logger.LogWarning("Agent {AgentKey} already registered, ignoring duplicate", agentKey);
            return Task.CompletedTask;
        }

        var entry = new AgentEntry
        {
            Info = new TrackedAgent(agentKey, label, parentAgentKey, AgentStatus.Pending, null, DateTime.UtcNow),
        };

        _agents[agentKey] = entry;
        _logger.LogInformation("Registered agent {AgentKey} (label: {Label})", agentKey, label);
        return Task.CompletedTask;
    }

    public async Task ReportCompletionAsync(string agentKey, AgentResult result, bool isError = false)
    {
        if (!_agents.TryGetValue(agentKey, out var entry))
        {
            _logger.LogWarning("ReportCompletion for unknown agent {AgentKey}", agentKey);
            return;
        }

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

        _logger.LogInformation("Agent {AgentKey} completed (status: {Status})", agentKey, newStatus);
        await DeliverToParentAsync(entry, agentKey, result, isError);
    }

    public async Task<AgentWaitResult?> WaitForNextAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);

        // Check for already-completed, undelivered agents (oldest first)
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

    #endregion

    #region Private Methods

    private async Task DeliverToParentAsync(AgentEntry entry, string agentKey, AgentResult result, bool isError)
    {
        try
        {
            var parentKey = entry.Info.ParentAgentKey;

            // Only deliver to conversation grains (they accept agent results via queue)
            if (!parentKey.StartsWith(AgentKeys.ConversationPrefix))
                return;

            var conversationGrain = GrainFactory.GetGrain<IConversationGrain>(parentKey);
            await conversationGrain.DeliverAgentResultAsync(
                agentKey,
                entry.Info.Label,
                isError ? result : null,
                isError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver completion for {AgentKey} to parent", agentKey);
        }
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

    #endregion

    #region Inner Types

    private sealed class AgentEntry
    {
        public TrackedAgent Info { get; set; } = null!;
        public TaskCompletionSource<AgentResult> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Delivered { get; set; }
    }

    #endregion
}
